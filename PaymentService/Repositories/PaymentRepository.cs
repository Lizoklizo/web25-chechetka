using Microsoft.EntityFrameworkCore;
using MQ.PaymentService.Models;

namespace MQ.PaymentService.Repositories;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment);
    Task<Payment?> GetByIdAsync(Guid id);
    Task<IEnumerable<Payment>> GetAllAsync();
    Task RemoveByIdAsync(Guid id);
}

public class PaymentRepository(ApplicationDbContext dbContext) : IPaymentRepository
{
    public async Task AddAsync(Payment payment)
    {
        payment.CreatedAt = DateTime.UtcNow;
        await dbContext.Payments.AddAsync(payment);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        return await dbContext.Payments.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Payment>> GetAllAsync()
    {
        return await dbContext.Payments.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync();   
    }
    
    public async Task RemoveByIdAsync(Guid id)
    {
        var payment = await dbContext.Payments.FindAsync(id);
        if (payment != null)
        {
            dbContext.Payments.Remove(payment);
            await dbContext.SaveChangesAsync();
        }
    }
}