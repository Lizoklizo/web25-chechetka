namespace MQ.UserService.Models;

// Модель исходящего сообщения (Outbox Pattern)
public class OutboxMessage
{
    // Уникальный идентификатор сообщения (ключ в таблице)
    public Guid Id { get; set; }

    // Тип события (например: "UserCreated", "UserUpdated", и т.д.)
    public string EventType { get; set; }

    // Сериализованное содержимое сообщения (обычно в JSON)
    public string Payload { get; set; }

    // Флаг, указывающий, было ли сообщение уже отправлено в очередь
    public bool Processed { get; set; }

    // Дата и время создания сообщения
    public DateTime CreatedAt { get; set; }
}
