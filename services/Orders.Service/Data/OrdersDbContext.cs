using Microsoft.EntityFrameworkCore;
using Orders.Service.Data.Entities;

namespace Orders.Service.Data;

/// <summary>
/// Хранилище данных заказов
/// </summary>
/// <param name="options"></param>
public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<OrderRow> Orders => Set<OrderRow>();
    public DbSet<OutboxRow> Outbox => Set<OutboxRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderRow>()
            .HasIndex(x => new { x.UserId, x.CreatedAtUtc });

        modelBuilder.Entity<OutboxRow>()
            .HasIndex(x => new { x.Topic, x.PublishedAtUtc });

        modelBuilder.Entity<OutboxRow>()
            .HasIndex(x => new { x.Topic, x.MessageKey })
            .IsUnique(false);
    }
}