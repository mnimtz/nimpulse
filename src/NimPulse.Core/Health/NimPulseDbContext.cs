using Microsoft.EntityFrameworkCore;

namespace NimPulse.Core.Health;

public class NimPulseDbContext(DbContextOptions<NimPulseDbContext> options) : DbContext(options)
{
    public DbSet<HealthSample> HealthSamples => Set<HealthSample>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HealthSample>(entity =>
        {
            entity.HasIndex(s => s.ExternalId).IsUnique();
            entity.HasIndex(s => new { s.Type, s.StartDate });
            entity.Property(s => s.Type).HasMaxLength(64);
            entity.Property(s => s.Unit).HasMaxLength(32);
            entity.Property(s => s.SourceName).HasMaxLength(128);
        });
    }
}
