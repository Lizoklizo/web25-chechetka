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
    // ����� �������� ������� (Hosted Service), ������� ������� ������� RabbitMQ
    public class RabbitMqWorker(                // ������� ����������� � RabbitMQ
        IConnectionFactory connectionFactory,   
        ILogger<RabbitMqWorker> logger,
        IServiceScopeFactory serviceScopeFactory)  // ��� ��������� ApplicationDbContext
        : IHostedService                           // ��������� �������� �������
    {
        private IConnection _connection;
        private IChannel _channel;
        private const string QueueName = "inbox_queue"; // �������� ������� �������� ���������


        // ����� ������� �������� ��������
        public async Task StartAsync(CancellationToken cancellationToken) // ������������� RabbitMQ
        {
            logger.LogInformation("Starting RabbitMQ Worker...");    // ��������� �����������

            await InitializeRabbitMqAsync(cancellationToken);
            await SetupConsumerAsync(cancellationToken);
        }

        // ����� ��������� �������
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping RabbitMQ Worker...");

            await Task.Yield();
            await _channel.CloseAsync(cancellationToken);      // �������� ������
            await _connection.CloseAsync(cancellationToken);   // �������� �����������
        }

        // ����������� � ������� � ���������� �������
        private async Task InitializeRabbitMqAsync(CancellationToken cancellationToken)
        {
            try
            {
                _connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                // ���������� �������, ���� ��� ��� �� �������
                await _channel.QueueDeclareAsync(QueueName, durable: false, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                // ��������� ������� ����������� ����� 5 ������
                logger.LogError($"Error initializing RabbitMQ: {ex.Message}");
                await Task.Delay(5000, cancellationToken);  // Retry logic
                await InitializeRabbitMqAsync(cancellationToken);  // Reconnect attempt
            }
        }

        // ��������� ���������� �� ������� RabbitMQ
        private async Task SetupConsumerAsync(CancellationToken cancellationToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, e) => await ProcessInboxMessageAsync(e, cancellationToken);
            await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken);  // ��������� ��������� ���������
        }

        // ������ ��� ������� ���������, �������� ����� ������
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
                // �� ������������ ���������, ��������� ��������
                await _channel.BasicNackAsync(e.DeliveryTag, false, true); // Retry the message
            }
        }

        // �������� ������ ��������� � ������ ��������� � ����
        public async Task ProcessInboxMessageAsync(string message, ulong deliveryTag, CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // ��������� ��������� ��� ��������
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

                // �������� ��� ������������
                inboxMessage.Processed = true;
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation($"Inbox message marked as processed: {inboxMessage.Id}");

                await transaction.CommitAsync(cancellationToken);

                // ������������ �������� �������
                await _channel.BasicAckAsync(deliveryTag, false);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError($"Error processing message: {ex.Message}");

                // ��������� ��������
                await _channel.BasicNackAsync(deliveryTag, false, true);
            }
        }
    }
}