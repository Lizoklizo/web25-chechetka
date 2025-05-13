namespace MQ.UserService.Models;

// Модель для входящих сообщений, реализующая Inbox Pattern
public class InboxMessage
{
    // Уникальный идентификатор сообщения (используется как первичный ключ)
    public Guid Id { get; set; }

    // Тип события 
    public string EventType { get; set; }

    // Содержимое сообщения
    public string Payload { get; set; }

    // Флаг, который показывает, было ли сообщение обработано
    public bool Processed { get; set; }

    // Дата и время получения или сохранения сообщения
    public DateTime CreatedAt { get; set; }
}
