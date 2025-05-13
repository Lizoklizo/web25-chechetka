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
    // ����� ������� ������, ����������� IHostedService ��� ������� ������ � �����������
    public class RabbitMqWorker(
        IConnectionFactory connectionFactory,              // ������� ����������� � RabbitMQ
        ILogger<RabbitMqWorker> logger,                    // ������ ��� ������ �������
        IServiceScopeFactory serviceScopeFactory)          // ��� ��������� ������������ �� ����������
        : IHostedService
    {
        private IConnection _connection;
        private IChannel _channel;

        // ������� ��� ��������� ��������� "UserCreated"
        private const string QueueName = "notification_created_queue";

        // ������ ������� ��� ������ ����������
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting RabbitMQ Worker...");

            await InitializeRabbitMqAsync(cancellationToken);  // ������������� �����������
            await SetupConsumerAsync(cancellationToken);        // ��������� �����������
        }

        // ��������� ������� ��� ���������� ������
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping RabbitMQ Worker...");

            await Task.Yield();
            await _channel.CloseAsync(cancellationToken);       // �������� ������
            await _connection.CloseAsync(cancellationToken);    // �������� ����������
        }

        // ����������� � RabbitMQ � ���������� �������
        private async Task InitializeRabbitMqAsync(CancellationToken cancellationToken)
        {
            try
            {
                // ������������� ���������� � �����
                _connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                // ��������� �������, ���� ��� �� ����������
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

                // ��������� ������� ����������� ����� 5 ������
                await Task.Delay(5000, cancellationToken);
                await InitializeRabbitMqAsync(cancellationToken);  // ������� ����������� �����
            }
        }

        // ��������� ����������� ������� RabbitMQ
        private async Task SetupConsumerAsync(CancellationToken cancellationToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            // ���������� ���������� ��� ��������� ���������
            consumer.ReceivedAsync += async (sender, e) => await ProcessUserCreatedEventAsync(e, cancellationToken);

            await _channel.BasicConsumeAsync(
                QueueName,
                autoAck: false, // ������ ������������� ���������
                consumer: consumer,
                cancellationToken: cancellationToken);
        }

        // ��������� ������ ��������� ���������
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

                // ���������� ��������� ������� � ������� ��� ��������� ���������
                await _channel.BasicNackAsync(e.DeliveryTag, false, true);
            }
        }

        // �������� ������ ��������� ������� "UserCreated"
        public async Task ProcessUserCreatedEventAsync(string message, ulong deliveryTag, CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            // ������������� ������� "UserCreated"
            var userCreatedEvent = JsonConvert.DeserializeObject<UserCreatedEvent>(message);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // ��������� ��������� ��� InboxMessage
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

                // �������� ��������� ��� ������������
                inboxMessage.Processed = true;
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation($"Inbox message marked as processed: {inboxMessage.Id}");

                // ������ ����������� ��� ������������
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = userCreatedEvent.Id,
                    Message = $"Welcome {userCreatedEvent.Name}, your account has been created."
                };

                // ���������� �����������
                await notificationService.SendNotificationAsync(notification);

                logger.LogInformation($"Notification sent to user {userCreatedEvent.Id}");

                await transaction.CommitAsync(cancellationToken);

                // ������������ �������� ��������� ���������
                await _channel.BasicAckAsync(deliveryTag, false);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError($"Error processing message: {ex.Message}");

                // ��������� ������� ��������� ���������
                await _channel.BasicNackAsync(deliveryTag, false, true);
            }
        }
    }
}