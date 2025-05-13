using Microsoft.EntityFrameworkCore;

namespace MQ.PaymentService.Models
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Payment> Payments { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }
        public DbSet<InboxMessage> InboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>();
            modelBuilder.Entity<OutboxMessage>();
            modelBuilder.Entity<InboxMessage>();
        }
    }
}