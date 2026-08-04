using Microsoft.EntityFrameworkCore;
using NimPulse.Core.Ai;
using NimPulse.Core.Settings;
using NimPulse.Core.Users;

namespace NimPulse.Core.Health;

public class NimPulseDbContext(DbContextOptions<NimPulseDbContext> options) : DbContext(options)
{
    public DbSet<HealthSample> HealthSamples => Set<HealthSample>();

    public DbSet<User> Users => Set<User>();

    public DbSet<AiGatewaySettings> AiGatewaySettings => Set<AiGatewaySettings>();

    public DbSet<StoredChatMessage> ChatMessages => Set<StoredChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HealthSample>(entity =>
        {
            entity.HasIndex(s => new { s.UserId, s.ExternalId }).IsUnique();
            entity.HasIndex(s => new { s.UserId, s.Type, s.StartDate });
            entity.Property(s => s.Type).HasMaxLength(64);
            entity.Property(s => s.Unit).HasMaxLength(32);
            entity.Property(s => s.SourceName).HasMaxLength(128);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.DisplayName).HasMaxLength(128);
        });

        modelBuilder.Entity<StoredChatMessage>(entity =>
        {
            entity.HasIndex(m => new { m.UserId, m.CreatedAt });
        });
    }
}
