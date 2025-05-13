using System.Text;
using MQ.NotificationService.Events;
using MQ.NotificationService.Models;
using MQ.NotificationService.Repositories;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MQ.NotificationService.Services;

// ��������� ������� �����������
public interface INotificationService
{
    Task<Notification> SendNotificationAsync(Notification notification);  // �������� �����������
    Task<Notification?> GetNotificationByIdAsync(Guid id); // ��������� �� ID
    Task<List<Notification>> GetAllNotificationsAsync();  // ��������� ���� �����������
    Task RemoveNotificatonByIdAsync(Guid id); // �������� �� ID
}

// ���������� ������� �����������
public class NotificationService(
    INotificationRepository notificationRepository,  // ����������� �����������
    IConnectionFactory connectionFactory,            // ����������� � RabbitMQ
    ApplicationDbContext dbContext)                  // �������� �� (��� Outbox)
    : INotificationService
{
    public async Task<Notification> SendNotificationAsync(Notification notification)
    {
        // ��������� ����������� ID � ���������� �����������
        notification.Id = Guid.NewGuid();
        await notificationRepository.AddAsync(notification);

        // ������������ Outbox-���������
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "NotificationSent",
            Payload = JsonConvert.SerializeObject(new { notification.Id, notification.Message }),
            Processed = false,
            CreatedAt = DateTime.UtcNow
        };

        // ��������� ��������� � ���� ������
        await dbContext.OutboxMessages.AddAsync(outboxMessage);
        await dbContext.SaveChangesAsync();

        // ����������� � RabbitMQ � ���������� �������
        var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        var body = Encoding.UTF8.GetBytes(outboxMessage.Payload);
        var basicProperties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        // ��������� ��������� � ������� "notification_sent_queue"
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

    // �������� ����������� �� ID
    public async Task<Notification?> GetNotificationByIdAsync(Guid id)
    {
        return await notificationRepository.GetByIdAsync(id);
    }

    // �������� ��� �����������
    public async Task<List<Notification>> GetAllNotificationsAsync()
    {
        return await notificationRepository.GetAllAsync();
    }

    // ������� ����������� �� ID
    public async Task RemoveNotificatonByIdAsync(Guid id)
    {
        await notificationRepository.RemoveByIdAsync(id);
    }
}