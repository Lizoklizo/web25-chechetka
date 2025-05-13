using System.Text;
using MQ.UserService.Models;
using MQ.UserService.Repositories;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace MQ.UserService.Services;

// интерфейс сервиса для работы с пользователями
public interface IUserService
{
    Task<User> CreateUserAsync(User user); // Cоздать пользователя
    Task<User?> GetUserByIdAsync(Guid id); // Получить пользователя по ID
    Task<IEnumerable<User>> GetAllUsersAsync(); // Получить всех пользователей
    Task RemoveUserByIdAsync(Guid id); // Удалить пользователя по ID
}

// Основная реализация сервиса пользователей
public class UserService(
    IUserRepository userRepository, // Репозиторий для операций с БД
    IConnectionFactory connectionFactory, // для подключения к RabbitMQ
    ApplicationDbContext dbContext)  // Контекст БД для Outbox-паттерна
    : IUserService
{
    public async Task<User> CreateUserAsync(User user)
    {
        // Присваиваем уникальный идентификатор и сохраняем пользователя
        user.Id = Guid.NewGuid();
        await userRepository.AddAsync(user);

        // Создаём сообщение Outbox
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "UserCreated",
            Payload = JsonConvert.SerializeObject(new { user.Id, user.Name, user.Email }),
            Processed = false, // Пока не обработано
            CreatedAt = DateTime.UtcNow
        };

        // Сохраняем Outbox-сообщение в базу
        await dbContext.OutboxMessages.AddAsync(outboxMessage);
        await dbContext.SaveChangesAsync();

        // Устанавливаем подключение к RabbitMQ
        var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var body = Encoding.UTF8.GetBytes(outboxMessage.Payload);
        var basicProperties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        // Публикуем сообщение в очередь user_created_queue
        var publicationAddress1 = new PublicationAddress(
            exchangeType: "direct",
            exchangeName: "",
            routingKey: "user_created_queue");

        await channel.BasicPublishAsync(
            publicationAddress1,
            basicProperties: basicProperties,
            body: body);

        // Публикуем то же сообщение в очередь notification_created_queue
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

    // Получить пользователя по ID
    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await userRepository.GetByIdAsync(id);
    }

    // Получить всех пользователей
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await userRepository.GetAllAsync();
    }

    // Удалить пользователя по ID
    public async Task RemoveUserByIdAsync(Guid id)
    {
        await userRepository.RemoveByIdAsync(id);
    }
}