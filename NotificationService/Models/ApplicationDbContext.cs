using Microsoft.EntityFrameworkCore;

namespace MQ.NotificationService.Models;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<InboxMessage> InboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>();
        modelBuilder.Entity<OutboxMessage>();
        modelBuilder.Entity<InboxMessage>();
    }
}