namespace MQ.PaymentService.Models;

// Модель входящего сообщения для реализации Inbox Pattern
public class InboxMessage
{
    // Уникальный идентификатор сообщения
    public Guid Id { get; set; }

    // Тип события 
    public string EventType { get; set; }

    // Полезная нагрузка события
    public string Payload { get; set; }

    // Было ли сообщение уже обработано
    public bool Processed { get; set; }

    // Время получения или сохранения сообщения
    public DateTime CreatedAt { get; set; }
}
