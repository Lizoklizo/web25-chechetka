namespace MQ.NotificationService.Models;

// Модель для уведомлений
public class Notification
{
    // Уникальный идентификатор уведомления
    public Guid Id { get; set; }

    // Дата и время создания уведомления
    public DateTime CreatedAt { get; set; }

    // Идентификатор пользователя, которому отправлено уведомление
    public Guid UserId { get; set; }

    // Сообщение, которое будет отправлено пользователю
    public string Message { get; set; }
}
