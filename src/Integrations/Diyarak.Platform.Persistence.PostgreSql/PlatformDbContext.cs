using Diyarak.Platform.Persistence.PostgreSql.Identity;
using Diyarak.Platform.Persistence.PostgreSql.Market;
using Microsoft.EntityFrameworkCore;

namespace Diyarak.Platform.Persistence.PostgreSql;

public sealed class PlatformDbContext(
    DbContextOptions<PlatformDbContext> options)
    : DbContext(options)
{
    internal DbSet<ExternalIdentityMappingRecord> ExternalIdentityMappings =>
        Set<ExternalIdentityMappingRecord>();

    internal DbSet<MarketListingRecord> MarketListings =>
        Set<MarketListingRecord>();

    internal DbSet<MarketPropertyRecord> MarketProperties =>
        Set<MarketPropertyRecord>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(
            new ExternalIdentityMappingRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new MarketListingRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new MarketPropertyRecordConfiguration());
    }
}
