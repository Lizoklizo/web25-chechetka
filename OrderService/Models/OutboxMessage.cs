namespace MQ.PaymentService.Models;

// Модель исходящего сообщения для Outbox Pattern
public class OutboxMessage
{
    // Уникальный идентификатор сообщения
    public Guid Id { get; set; }

    // Тип события 
    public string EventType { get; set; }

    // Полезная нагрузка
    public string Payload { get; set; }

    // Флаг, указывающий, было ли сообщение уже отправлено в очередь
    public bool Processed { get; set; }

    // Дата и время создания события
    public DateTime CreatedAt { get; set; }
}
