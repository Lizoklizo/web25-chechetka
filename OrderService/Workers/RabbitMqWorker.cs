using System.Text;
using MQ.PaymentService.Events;
using MQ.PaymentService.Models;
using MQ.PaymentService.Services;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MQ.PaymentService.Workers
{
    // ������� ������, ����������� IHostedService � ����������� ������ � �����������
    public class RabbitMqWorker(
        IConnectionFactory connectionFactory,            // ������� ����������� � RabbitMQ
        ILogger<RabbitMqWorker> logger,                  // ������
        IServiceScopeFactory serviceScopeFactory)        // ��� ��������� ������������ �� ����������
        : IHostedService
    {
        private IConnection _connection;
        private IChannel _channel;

        // �������, ������� ������� � ���� ����������� OrderCreated
        private const string QueueName = "order_created_queue";

        // ������ ������� ��� ������ ����������
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting RabbitMQ Worker...");

            await InitializeRabbitMqAsync(cancellationToken);
            await SetupConsumerAsync(cancellationToken);
        }

        // ��������� ������� � �������� ����������
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping RabbitMQ Worker...");

            await Task.Yield();
            await _channel.CloseAsync(cancellationToken);
            await _connection.CloseAsync(cancellationToken);
        }

        // ����������� � RabbitMQ � ���������� �������
        private async Task InitializeRabbitMqAsync(CancellationToken cancellationToken)
        {
            try
            {
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
                await InitializeRabbitMqAsync(cancellationToken);
            }
        }

        // ��������� ����������� ��������� �� �������
        private async Task SetupConsumerAsync(CancellationToken cancellationToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            // ���������� ���������� ������� ��������� ���������
            consumer.ReceivedAsync += async (sender, e) =>
                await ProcessOrderCreatedEventAsync(e, cancellationToken);

            await _channel.BasicConsumeAsync(
                QueueName,
                autoAck: false, // ������ ������������� ��������
                consumer: consumer,
                cancellationToken: cancellationToken);
        }

        // ��������� ������ ��������� ��������� (������ � �������������)
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

                // ��������� �������� ���������, ���� ��������� ������
                await _channel.BasicNackAsync(e.DeliveryTag, false, true);
            }
        }

        // �������� ������ ��������� ������� "OrderCreated"
        public async Task ProcessOrderCreatedEventAsync(string message, ulong deliveryTag, CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

            // ������������� �������
            var orderCreatedEvent = JsonConvert.DeserializeObject<OrderCreatedEvent>(message);

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // ��������� ��������� � ���� ��� InboxMessage
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

                // �������� ��������� ��� ������������
                inboxMessage.Processed = true;
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation($"Inbox message marked as processed: {inboxMessage.Id}");

                // ������ ����� �� ������ ������
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderCreatedEvent.Id,
                    Amount = orderCreatedEvent.TotalAmount,
                    PaymentStatus = "Processed"
                };

                // ���������� ����� � PaymentService (�� ��� ������� Outbox-���������)
                await paymentService.ProcessPaymentAsync(payment);

                logger.LogInformation($"Payment processed for order {orderCreatedEvent.Id}");

                await transaction.CommitAsync(cancellationToken);

                // ������������ �������� ��������� �������
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