using System.Text;
using MQ.PaymentService.Events;
using MQ.PaymentService.Models;
using MQ.PaymentService.Services;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MQ.PaymentService.Workers
{
    // Фоновый воркер, реализующий IHostedService — запускается вместе с приложением
    public class RabbitMqWorker(
        IConnectionFactory connectionFactory,            // Фабрика подключения к RabbitMQ
        ILogger<RabbitMqWorker> logger,                  // Логгер
        IServiceScopeFactory serviceScopeFactory)        // Для получения зависимостей из контейнера
        : IHostedService
    {
        private IConnection _connection;
        private IChannel _channel;

        // Очередь, которую слушаем — сюда публикуется OrderCreated
        private const string QueueName = "order_created_queue";

        // Запуск воркера при старте приложения
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting RabbitMQ Worker...");

            await InitializeRabbitMqAsync(cancellationToken);
            await SetupConsumerAsync(cancellationToken);
        }

        // Остановка воркера и закрытие соединений
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping RabbitMQ Worker...");

            await Task.Yield();
            await _channel.CloseAsync(cancellationToken);
            await _connection.CloseAsync(cancellationToken);
        }

        // Подключение к RabbitMQ и объявление очереди
        private async Task InitializeRabbitMqAsync(CancellationToken cancellationToken)
        {
            try
            {
                _connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                // Объявляем очередь, если она не существует
                await _channel.QueueDeclareAsync(
                    QueueName,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error initializing RabbitMQ: {ex.Message}");

                // Повторная попытка подключения через 5 секунд
                await Task.Delay(5000, cancellationToken);
                await InitializeRabbitMqAsync(cancellationToken);
            }
        }

        // Настройка потребителя сообщений из очереди
        private async Task SetupConsumerAsync(CancellationToken cancellationToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            // Подключаем обработчик события получения сообщения
            consumer.ReceivedAsync += async (sender, e) =>
                await ProcessOrderCreatedEventAsync(e, cancellationToken);

            await _channel.BasicConsumeAsync(
                QueueName,
                autoAck: false, // Ручное подтверждение доставки
                consumer: consumer,
                cancellationToken: cancellationToken);
        }

        // Обработка одного входящего сообщения (обёртка с безопасностью)
        private async Task ProcessOrderCreatedEventAsync(BasicDeliverEventArgs e, CancellationToken cancellationToken)
        {
            var body = e.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                await ProcessOrderCreatedEventAsync(message, e.DeliveryTag, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in message handler: {ex.Message}");

                // Повторная доставка сообщения, если произошла ошибка
                await _channel.BasicNackAsync(e.DeliveryTag, false, true);
            }
        }

        // Основная логика обработки события "OrderCreated"
        public async Task ProcessOrderCreatedEventAsync(string message, ulong deliveryTag, CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

            // Десериализуем событие
            var orderCreatedEvent = JsonConvert.DeserializeObject<OrderCreatedEvent>(message);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Сохраняем сообщение в базу как InboxMessage
                var inboxMessage = new InboxMessage
                {
                    Id = Guid.NewGuid(),
                    EventType = "OrderCreated",
                    Payload = message,
                    Processed = false,
                    CreatedAt = DateTime.UtcNow
                };

                await dbContext.InboxMessages.AddAsync(inboxMessage, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation($"Inbox message added: {inboxMessage.Id}");

                // Помечаем сообщение как обработанное
                inboxMessage.Processed = true;
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation($"Inbox message marked as processed: {inboxMessage.Id}");

                // Создаём платёж на основе заказа
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderCreatedEvent.Id,
                    Amount = orderCreatedEvent.TotalAmount,
                    PaymentStatus = "Processed"
                };

                // Отправляем платёж в PaymentService (он сам создаст Outbox-сообщение)
                await paymentService.ProcessPaymentAsync(payment);

                logger.LogInformation($"Payment processed for order {orderCreatedEvent.Id}");

                await transaction.CommitAsync(cancellationToken);

                // Подтверждаем доставку сообщения брокеру
                await _channel.BasicAckAsync(deliveryTag, false);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError($"Error processing message: {ex.Message}");

                // Повторная доставка
                await _channel.BasicNackAsync(deliveryTag, false, true);
            }
        }
    }
}