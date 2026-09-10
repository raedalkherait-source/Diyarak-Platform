using Diyarak.Platform.Persistence.PostgreSql.Market;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql;

public sealed class PlatformDbContext(
    DbContextOptions<PlatformDbContext> options)
    : DbContext(options)
{
    internal DbSet<MarketPropertyRecord> MarketProperties =>
        Set<MarketPropertyRecord>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(
            new MarketPropertyRecordConfiguration());
    }
}
