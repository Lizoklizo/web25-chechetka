using System.Text;
using MQ.NotificationService.Models;
using MQ.NotificationService.Services;
using MQ.NotificationService.Events;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace MQ.NotificationService.Workers
{
    // Класс фоновый воркер, реализующий IHostedService для запуска вместе с приложением
    public class RabbitMqWorker(
        IConnectionFactory connectionFactory,              // Фабрика подключения к RabbitMQ
        ILogger<RabbitMqWorker> logger,                    // Логгер для записи событий
        IServiceScopeFactory serviceScopeFactory)          // Для получения зависимостей из контейнера
        : IHostedService
    {
        private IConnection _connection;
        private IChannel _channel;

        // Очередь для получения сообщений "UserCreated"
        private const string QueueName = "notification_created_queue";

        // Запуск воркера при старте приложения
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting RabbitMQ Worker...");

            await InitializeRabbitMqAsync(cancellationToken);  // Инициализация подключения
            await SetupConsumerAsync(cancellationToken);        // Настройка потребителя
        }

        // Остановка воркера при завершении работы
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping RabbitMQ Worker...");

            await Task.Yield();
            await _channel.CloseAsync(cancellationToken);       // Закрытие канала
            await _connection.CloseAsync(cancellationToken);    // Закрытие соединения
        }

        // Подключение к RabbitMQ и объявление очереди
        private async Task InitializeRabbitMqAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Устанавливаем соединение и канал
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
                await InitializeRabbitMqAsync(cancellationToken);  // Попытка подключения снова
            }
        }

        // Настройка потребителя очереди RabbitMQ
        private async Task SetupConsumerAsync(CancellationToken cancellationToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            // Подключаем обработчик для получения сообщений
            consumer.ReceivedAsync += async (sender, e) => await ProcessUserCreatedEventAsync(e, cancellationToken);

            await _channel.BasicConsumeAsync(
                QueueName,
                autoAck: false, // Ручное подтверждение сообщений
                consumer: consumer,
                cancellationToken: cancellationToken);
        }

        // Обработка одного входящего сообщения
        private async Task ProcessUserCreatedEventAsync(BasicDeliverEventArgs e, CancellationToken cancellationToken)
        {
            var body = e.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                await ProcessUserCreatedEventAsync(message, e.DeliveryTag, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error in message handler: {ex.Message}");

                // Отправляем сообщение обратно в очередь для повторной обработки
                await _channel.BasicNackAsync(e.DeliveryTag, false, true);
            }
        }

        // Основная логика обработки события "UserCreated"
        public async Task ProcessUserCreatedEventAsync(string message, ulong deliveryTag, CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            // Десериализуем событие "UserCreated"
            var userCreatedEvent = JsonConvert.DeserializeObject<UserCreatedEvent>(message);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Сохраняем сообщение как InboxMessage
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

                // Помечаем сообщение как обработанное
                inboxMessage.Processed = true;
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation($"Inbox message marked as processed: {inboxMessage.Id}");

                // Создаём уведомление для пользователя
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userCreatedEvent.Id,
                    Message = $"Welcome {userCreatedEvent.Name}, your account has been created."
                };

                // Отправляем уведомление
                await notificationService.SendNotificationAsync(notification);

                logger.LogInformation($"Notification sent to user {userCreatedEvent.Id}");

                await transaction.CommitAsync(cancellationToken);

                // Подтверждаем успешную обработку сообщения
                await _channel.BasicAckAsync(deliveryTag, false);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError($"Error processing message: {ex.Message}");

                // Повторная попытка обработки сообщения
                await _channel.BasicNackAsync(deliveryTag, false, true);
            }
        }
    }
}