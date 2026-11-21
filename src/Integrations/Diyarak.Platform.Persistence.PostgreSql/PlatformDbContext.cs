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

    internal DbSet<PhoneVerificationChallengeRecord> PhoneVerificationChallenges =>
        Set<PhoneVerificationChallengeRecord>();

    internal DbSet<PhoneAccountRecord> PhoneAccounts =>
        Set<PhoneAccountRecord>();

    internal DbSet<UserSessionRecord> UserSessions =>
        Set<UserSessionRecord>();

    internal DbSet<MarketListingRecord> MarketListings =>
        Set<MarketListingRecord>();

    internal DbSet<MarketListingPhotoRecord> MarketListingPhotos =>
        Set<MarketListingPhotoRecord>();

    internal DbSet<MarketListingReportRecord> MarketListingReports =>
        Set<MarketListingReportRecord>();

    internal DbSet<MarketListingFavoriteRecord> MarketListingFavorites =>
        Set<MarketListingFavoriteRecord>();

    internal DbSet<MarketSavedSearchRecord> MarketSavedSearches =>
        Set<MarketSavedSearchRecord>();

    internal DbSet<MarketListingDailyStatisticsRecord> MarketListingDailyStatistics =>
        Set<MarketListingDailyStatisticsRecord>();

    internal DbSet<MarketAgencyRecord> MarketAgencies =>
        Set<MarketAgencyRecord>();

    internal DbSet<MarketPropertyRecord> MarketProperties =>
        Set<MarketPropertyRecord>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(
            new ExternalIdentityMappingRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new PhoneVerificationChallengeRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new PhoneAccountRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new UserSessionRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new MarketListingRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new MarketPropertyRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new MarketListingPhotoRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new MarketListingReportRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new MarketListingFavoriteRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new MarketSavedSearchRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new MarketListingDailyStatisticsRecordConfiguration());

        modelBuilder.ApplyConfiguration(
            new MarketAgencyRecordConfiguration());
    }
}
