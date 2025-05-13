namespace MQ.NotificationService.Models;

// Модель для исходящих сообщений, реализующая Outbox Pattern
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

    // Дата и время создания сообщения
    public DateTime CreatedAt { get; set; }
}
