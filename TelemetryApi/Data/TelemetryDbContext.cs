using Microsoft.EntityFrameworkCore;
using TelemetryApi.Models;

namespace TelemetryApi.Data
{
    public class TelemetryDbContext : DbContext
    {
        public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : base(options) { }
        public DbSet<TelemetryRecord> TelemetryRecords => Set<TelemetryRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TelemetryRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.DeviceId, e.TimestampUtc });
                entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).HasMaxLength(20);
            });
        }
    }
}
