using Microsoft.EntityFrameworkCore;
using MQ.UserService.Models;

namespace MQ.UserService.Repositories;

public interface IUserRepository
{
    Task AddAsync(User user);
    Task<User?> GetByIdAsync(Guid id);
    Task<IEnumerable<User>> GetAllAsync();
    Task RemoveByIdAsync(Guid id);
}

public class UserRepository(ApplicationDbContext dbContext) : IUserRepository
{
    public async Task AddAsync(User user)
    {
        user.CreatedAt = DateTime.UtcNow;
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await dbContext.Users.FindAsync(id);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await dbContext.Users.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync();
    }
    
    public async Task RemoveByIdAsync(Guid id)
    {
        var user = await dbContext.Users.FindAsync(id);
        if (user != null)
        {
            dbContext.Users.Remove(user);
            await dbContext.SaveChangesAsync();
        }
    }
}