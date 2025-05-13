namespace MQ.NotificationService.Models;

// Модель для входящих сообщений, реализующая Inbox Pattern
public class InboxMessage
{
    // Уникальный идентификатор сообщения
    public Guid Id { get; set; }

    // Тип события 
    public string EventType { get; set; }

    // Полезная нагрузка 
    public string Payload { get; set; }

    // Флаг, указывающий, было ли сообщение уже обработано
    public bool Processed { get; set; }

    // Дата и время получения сообщения
    public DateTime CreatedAt { get; set; }
}
