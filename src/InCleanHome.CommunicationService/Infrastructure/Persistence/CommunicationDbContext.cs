using EntityFrameworkCore.CreatedUpdatedDate.Extensions;
using InCleanHome.CommunicationService.Infrastructure.Persistence.Extensions;
using InCleanHome.CommunicationService.Messaging.Domain.Model.Aggregates;
using InCleanHome.CommunicationService.Notifications.Domain.Model.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.CommunicationService.Infrastructure.Persistence;

/// <summary>
/// Single DbContext for the Communication microservice. Tables from both
/// internal bounded contexts (Notifications + Messaging) coexist in the same
/// PostgreSQL database (communication_db). They don't cross-reference each
/// other in the schema — independence is enforced only by convention.
/// </summary>
public class CommunicationDbContext(DbContextOptions<CommunicationDbContext> options) : DbContext(options)
{
    // Notifications BC
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();

    // Messaging BC
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.AddCreatedUpdatedInterceptor();
        base.OnConfiguring(builder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Notification
        builder.Entity<Notification>().HasKey(n => n.Id);
        builder.Entity<Notification>().Property(n => n.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<Notification>().Property(n => n.UserId).IsRequired();
        builder.Entity<Notification>().Property(n => n.Type).IsRequired().HasMaxLength(40);
        builder.Entity<Notification>().Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Entity<Notification>().Property(n => n.Body).IsRequired().HasMaxLength(2000);
        builder.Entity<Notification>().Property(n => n.Link).HasMaxLength(500);
        builder.Entity<Notification>().Property(n => n.Read).HasDefaultValue(false);
        builder.Entity<Notification>().Property(n => n.IdempotencyKey).HasMaxLength(120);
        builder.Entity<Notification>().HasIndex(n => n.UserId);
        builder.Entity<Notification>().HasIndex(n => new { n.UserId, n.Read });
        // Unique index over IdempotencyKey when set: prevents duplicate
        // notifications when both the broker delivery and the HTTP fallback
        // arrive for the same domain event.
        builder.Entity<Notification>().HasIndex(n => n.IdempotencyKey).IsUnique()
            .HasFilter("\"idempotency_key\" IS NOT NULL");

        // UserDevice
        builder.Entity<UserDevice>().HasKey(d => d.Id);
        builder.Entity<UserDevice>().Property(d => d.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<UserDevice>().Property(d => d.UserId).IsRequired();
        builder.Entity<UserDevice>().Property(d => d.Token).IsRequired().HasMaxLength(500);
        builder.Entity<UserDevice>().Property(d => d.Role).HasMaxLength(20);
        builder.Entity<UserDevice>().HasIndex(d => d.UserId).IsUnique();

        // Message
        builder.Entity<Message>().HasKey(m => m.Id);
        builder.Entity<Message>().Property(m => m.Id).IsRequired().ValueGeneratedOnAdd();
        builder.Entity<Message>().Property(m => m.SenderId).IsRequired();
        builder.Entity<Message>().Property(m => m.RecipientId).IsRequired();
        builder.Entity<Message>().Property(m => m.Content).IsRequired().HasMaxLength(5000);
        builder.Entity<Message>().HasIndex(m => new { m.SenderId, m.RecipientId });
        builder.Entity<Message>().HasIndex(m => m.RecipientId);

        builder.UseSnakeCaseNamingConvention();
    }
}
