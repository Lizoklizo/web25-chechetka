using System.Text;
using MQ.PaymentService.Models;
using MQ.PaymentService.Repositories;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace MQ.PaymentService.Services;

// ��������� ������� ��������� ��������
public interface IPaymentService
{
    Task<Payment> ProcessPaymentAsync(Payment payment); // ��������� �������
    Task<Payment?> GetPaymentByIdAsync(Guid id);  // ��������� ������� �� ID
    Task<IEnumerable<Payment>> GetAllPaymentsAsync();  // ��������� ���� ��������
    Task RemovePaymentByIdAsync(Guid id); // �������� ������� �� ID
}

// ���������� ������� ��������� ��������
public class PaymentService(
    IPaymentRepository paymentRepository,  // ����������� ��������
    IConnectionFactory connectionFactory,  // ����������� � RabbitMQ
    ApplicationDbContext dbContext)        // �������� ��� ������ � Outbox
    : IPaymentService     
{
    public async Task<Payment> ProcessPaymentAsync(Payment payment)
    {
        // ���������� ID � ��������� �����
        payment.Id = Guid.NewGuid();
        await paymentRepository.AddAsync(payment);

        // ������ Outbox-���������
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "PaymentProcessed",
            Payload = JsonConvert.SerializeObject(new { payment.Id, payment.Amount }),
            Processed = false,
            CreatedAt = DateTime.UtcNow
        };

        // ��������� ��������� � ������� Outbox
        await dbContext.OutboxMessages.AddAsync(outboxMessage);
        await dbContext.SaveChangesAsync();

        // ������������ � RabbitMQ � ������ �����
        var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // ��������� ��������� � ��� ��������
        var body = Encoding.UTF8.GetBytes(outboxMessage.Payload);
        var basicProperties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent // ��������� ����������� ��� �����
        };

        // ��������� ��������� � ������� payment_processed_queue
        var publicationAddress = new PublicationAddress(
            exchangeType: "direct",
            exchangeName: "",
            routingKey: "payment_processed_queue");

        await channel.BasicPublishAsync(
            publicationAddress,
            basicProperties: basicProperties,
            body: body);

        return payment;
    }

    // �������� ����� �� ID
    public async Task<Payment?> GetPaymentByIdAsync(Guid id)
    {
        return await paymentRepository.GetByIdAsync(id);
    }

    // �������� ��� �������
    public async Task<IEnumerable<Payment>> GetAllPaymentsAsync()
    {
        return await paymentRepository.GetAllAsync();
    }

    // ������� ����� �� ID
    public async Task RemovePaymentByIdAsync(Guid id)
    {
        await paymentRepository.RemoveByIdAsync(id);
    }
}