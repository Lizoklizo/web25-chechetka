using System.Text;
using MQ.UserService.Models;
using MQ.UserService.Repositories;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace MQ.UserService.Services;

// ��������� ������� ��� ������ � ��������������
public interface IUserService
{
    Task<User> CreateUserAsync(User user); // C������ ������������
    Task<User?> GetUserByIdAsync(Guid id); // �������� ������������ �� ID
    Task<IEnumerable<User>> GetAllUsersAsync(); // �������� ���� �������������
    Task RemoveUserByIdAsync(Guid id); // ������� ������������ �� ID
}

// �������� ���������� ������� �������������
public class UserService(
    IUserRepository userRepository, // ����������� ��� �������� � ��
    IConnectionFactory connectionFactory, // ��� ����������� � RabbitMQ
    ApplicationDbContext dbContext)  // �������� �� ��� Outbox-��������
    : IUserService
{
    public async Task<User> CreateUserAsync(User user)
    {
        // ����������� ���������� ������������� � ��������� ������������
        user.Id = Guid.NewGuid();
        await userRepository.AddAsync(user);

        // ������ ��������� Outbox
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "UserCreated",
            Payload = JsonConvert.SerializeObject(new { user.Id, user.Name, user.Email }),
            Processed = false, // ���� �� ����������
            CreatedAt = DateTime.UtcNow
        };

        // ��������� Outbox-��������� � ����
        await dbContext.OutboxMessages.AddAsync(outboxMessage);
        await dbContext.SaveChangesAsync();

        // ������������� ����������� � RabbitMQ
        var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var body = Encoding.UTF8.GetBytes(outboxMessage.Payload);
        var basicProperties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        // ��������� ��������� � ������� user_created_queue
        var publicationAddress1 = new PublicationAddress(
            exchangeType: "direct",
            exchangeName: "",
            routingKey: "user_created_queue");

        await channel.BasicPublishAsync(
            publicationAddress1,
            basicProperties: basicProperties,
            body: body);

        // ��������� �� �� ��������� � ������� notification_created_queue
        var publicationAddress2 = new PublicationAddress(
            exchangeType: "direct",
            exchangeName: "",
            routingKey: "notification_created_queue");

        await channel.BasicPublishAsync(
            publicationAddress2,
            basicProperties: basicProperties,
            body: body);

        return user;
    }

    // �������� ������������ �� ID
    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await userRepository.GetByIdAsync(id);
    }

    // �������� ���� �������������
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await userRepository.GetAllAsync();
    }

    // ������� ������������ �� ID
    public async Task RemoveUserByIdAsync(Guid id)
    {
        await userRepository.RemoveByIdAsync(id);
    }
}