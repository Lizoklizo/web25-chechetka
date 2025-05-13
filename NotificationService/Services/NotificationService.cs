using System.Text;
using MQ.NotificationService.Events;
using MQ.NotificationService.Models;
using MQ.NotificationService.Repositories;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MQ.NotificationService.Services;

// Интерфейс сервиса уведомлений
public interface INotificationService
{
    Task<Notification> SendNotificationAsync(Notification notification);  // Отправка уведомления
    Task<Notification?> GetNotificationByIdAsync(Guid id); // Получение по ID
    Task<List<Notification>> GetAllNotificationsAsync();  // Получение всех уведомлений
    Task RemoveNotificatonByIdAsync(Guid id); // Удаление по ID
}

// Реализация сервиса уведомлений
public class NotificationService(
    INotificationRepository notificationRepository,  // Репозиторий уведомлений
    IConnectionFactory connectionFactory,            // Подключение к RabbitMQ
    ApplicationDbContext dbContext)                  // Контекст БД (для Outbox)
    : INotificationService
{
    public async Task<Notification> SendNotificationAsync(Notification notification)
    {
        // Генерация уникального ID и сохранение уведомления
        notification.Id = Guid.NewGuid();
        await notificationRepository.AddAsync(notification);

        // Формирование Outbox-сообщения
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "NotificationSent",
            Payload = JsonConvert.SerializeObject(new { notification.Id, notification.Message }),
            Processed = false,
            CreatedAt = DateTime.UtcNow
        };

        // Сохраняем сообщение в базу данных
        await dbContext.OutboxMessages.AddAsync(outboxMessage);
        await dbContext.SaveChangesAsync();

        // Подключение к RabbitMQ и публикация события
        var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        var body = Encoding.UTF8.GetBytes(outboxMessage.Payload);
        var basicProperties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        // Публикуем сообщение в очередь "notification_sent_queue"
        var publicationAddress = new PublicationAddress(
            exchangeType: "direct",
            exchangeName: "",
            routingKey: "notification_sent_queue");

        await channel.BasicPublishAsync(
            publicationAddress,
            basicProperties: basicProperties,
            body: body);

        return notification;
    }

    // Получить уведомление по ID
    public async Task<Notification?> GetNotificationByIdAsync(Guid id)
    {
        return await notificationRepository.GetByIdAsync(id);
    }

    // Получить все уведомления
    public async Task<List<Notification>> GetAllNotificationsAsync()
    {
        return await notificationRepository.GetAllAsync();
    }

    // Удалить уведомление по ID
    public async Task RemoveNotificatonByIdAsync(Guid id)
    {
        await notificationRepository.RemoveByIdAsync(id);
    }
}