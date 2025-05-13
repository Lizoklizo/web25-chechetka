using Microsoft.EntityFrameworkCore;
using MQ.NotificationService.Models;

namespace MQ.NotificationService.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<Notification?> GetByIdAsync(Guid id);
    Task<List<Notification>> GetAllAsync();
    Task RemoveByIdAsync(Guid id);
}

public class NotificationRepository(ApplicationDbContext dbContext) : INotificationRepository
{
    public async Task AddAsync(Notification notification)
    {
        notification.CreatedAt = DateTime.UtcNow;
        await dbContext.Notifications.AddAsync(notification);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Notification?> GetByIdAsync(Guid id)
    {
        return await dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<List<Notification>> GetAllAsync()
    {
        return await dbContext.Notifications.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync();
    }
    
    public async Task RemoveByIdAsync(Guid id)
    {
        var notification = await dbContext.Notifications.FindAsync(id);
        if (notification != null)
        {
            dbContext.Notifications.Remove(notification);
            
            await dbContext.SaveChangesAsync();
        }
    }
}