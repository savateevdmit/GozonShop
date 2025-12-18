using Microsoft.EntityFrameworkCore;
using Payments.Service.Data.Entities;

namespace Payments.Service.Data;

/// <summary>
/// Контекст базы данных платежного сервиса
/// </summary>
public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<AccountRow> Accounts => Set<AccountRow>();
    public DbSet<InboxRow> Inbox => Set<InboxRow>();
    public DbSet<OutboxRow> Outbox => Set<OutboxRow>();
    public DbSet<LedgerRow> Ledger => Set<LedgerRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountRow>()
            .HasIndex(x => x.UserId)
            .IsUnique();

        modelBuilder.Entity<InboxRow>()
            .HasIndex(x => new { x.Topic, x.MessageKey })
            .IsUnique();

        // idempotency for ledger operations
        modelBuilder.Entity<LedgerRow>()
            .HasIndex(x => new { x.Kind, x.ExternalRef })
            .IsUnique();

        modelBuilder.Entity<OutboxRow>()
            .HasIndex(x => new { x.Topic, x.PublishedAtUtc });
    }
}