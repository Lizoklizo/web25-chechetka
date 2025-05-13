using System.Text;
using MQ.UserService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace MQ.UserService.Workers
{
    // Класс фонового сервиса (Hosted Service), который слушает очередь RabbitMQ
    public class RabbitMqWorker(                // Фабрика подключения к RabbitMQ
        IConnectionFactory connectionFactory,   
        ILogger<RabbitMqWorker> logger,
        IServiceScopeFactory serviceScopeFactory)  // Для получения ApplicationDbContext
        : IHostedService                           // Интерфейс фонового сервиса
    {
        private IConnection _connection;
        private IChannel _channel;
        private const string QueueName = "inbox_queue"; // Название очереди входящих сообщений


        // Метод запуска фонового процесса
        public async Task StartAsync(CancellationToken cancellationToken) // Инициализация RabbitMQ
        {
            logger.LogInformation("Starting RabbitMQ Worker...");    // Настройка потребителя

            await InitializeRabbitMqAsync(cancellationToken);
            await SetupConsumerAsync(cancellationToken);
        }

        // Метод остановки сервиса
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping RabbitMQ Worker...");

            await Task.Yield();
            await _channel.CloseAsync(cancellationToken);      // Закрытие канала
            await _connection.CloseAsync(cancellationToken);   // Закрытие подключения
        }

        // Подключение к брокеру и объявление очереди
        private async Task InitializeRabbitMqAsync(CancellationToken cancellationToken)
        {
            try
            {
                _connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                // Объявление очереди, если она ещё не создана
                await _channel.QueueDeclareAsync(QueueName, durable: false, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                // Повторная попытка подключения через 5 секунд
                logger.LogError($"Error initializing RabbitMQ: {ex.Message}");
                await Task.Delay(5000, cancellationToken);  // Retry logic
                await InitializeRabbitMqAsync(cancellationToken);  // Reconnect attempt
            }
        }

        // Настройка подписчика на очередь RabbitMQ
        private async Task SetupConsumerAsync(CancellationToken cancellationToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, e) => await ProcessInboxMessageAsync(e, cancellationToken);
            await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken);  // Обработка входящего сообщения
        }

        // Обёртка над логикой обработки, включает отлов ошибок
        private async Task ProcessInboxMessageAsync(BasicDeliverEventArgs e, CancellationToken cancellationToken)
        {
            var body = e.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                await ProcessInboxMessageAsync(message, e.DeliveryTag, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in message handler: {ex.Message}");
                // Не подтверждаем сообщение, повторная доставка
                await _channel.BasicNackAsync(e.DeliveryTag, false, true); // Retry the message
            }
        }

        // Основная логика обработки и записи сообщения в базу
        public async Task ProcessInboxMessageAsync(string message, ulong deliveryTag, CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Сохраняем сообщение как входящее
                var inboxMessage = new InboxMessage
                {
                    Id = Guid.NewGuid(),
                    EventType = "UserCreated",
                    Payload = message,
                    Processed = false,
                    CreatedAt = DateTime.UtcNow
                };

                await dbContext.InboxMessages.AddAsync(inboxMessage, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation($"Inbox message added: {inboxMessage.Id}");

                // Помечаем как обработанное
                inboxMessage.Processed = true;
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation($"Inbox message marked as processed: {inboxMessage.Id}");

                await transaction.CommitAsync(cancellationToken);

                // Подтверждаем доставку брокеру
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