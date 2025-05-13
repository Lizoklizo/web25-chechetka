using System.Text;
using MQ.PaymentService.Models;
using MQ.PaymentService.Repositories;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace MQ.PaymentService.Services;

// Интерфейс сервиса обработки платежей
public interface IPaymentService
{
    Task<Payment> ProcessPaymentAsync(Payment payment); // Обработка платежа
    Task<Payment?> GetPaymentByIdAsync(Guid id);  // Получение платежа по ID
    Task<IEnumerable<Payment>> GetAllPaymentsAsync();  // Получение всех платежей
    Task RemovePaymentByIdAsync(Guid id); // Удаление платежа по ID
}

// Реализация сервиса обработки платежей
public class PaymentService(
    IPaymentRepository paymentRepository,  // Репозиторий платежей
    IConnectionFactory connectionFactory,  // Подключение к RabbitMQ
    ApplicationDbContext dbContext)        // Контекст для работы с Outbox
    : IPaymentService     
{
    public async Task<Payment> ProcessPaymentAsync(Payment payment)
    {
        // Генерируем ID и сохраняем платёж
        payment.Id = Guid.NewGuid();
        await paymentRepository.AddAsync(payment);

        // Создаём Outbox-сообщение
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "PaymentProcessed",
            Payload = JsonConvert.SerializeObject(new { payment.Id, payment.Amount }),
            Processed = false,
            CreatedAt = DateTime.UtcNow
        };

        // Сохраняем сообщение в таблицу Outbox
        await dbContext.OutboxMessages.AddAsync(outboxMessage);
        await dbContext.SaveChangesAsync();

        // Подключаемся к RabbitMQ и создаём канал
        var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // Формируем сообщение и его свойства
        var body = Encoding.UTF8.GetBytes(outboxMessage.Payload);
        var basicProperties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent // Сообщение сохраняется при сбоях
        };

        // Публикуем сообщение в очередь payment_processed_queue
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

    // Получить платёж по ID
    public async Task<Payment?> GetPaymentByIdAsync(Guid id)
    {
        return await paymentRepository.GetByIdAsync(id);
    }

    // Получить все платежи
    public async Task<IEnumerable<Payment>> GetAllPaymentsAsync()
    {
        return await paymentRepository.GetAllAsync();
    }

    // Удалить платёж по ID
    public async Task RemovePaymentByIdAsync(Guid id)
    {
        await paymentRepository.RemoveByIdAsync(id);
    }
}