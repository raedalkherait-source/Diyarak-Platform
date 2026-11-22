using Diyarak.Market.Application;
using Diyarak.Market.Application.Moderation;
using Diyarak.Market.Application.Photos;
using Diyarak.Market.Application.PriceReports;
using Diyarak.Market.Application.Reviews;
using Diyarak.Platform.Identity;
using Diyarak.Platform.Identity.Application;
using Diyarak.Platform.Persistence.PostgreSql.Identity;
using Diyarak.Platform.Persistence.PostgreSql.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Diyarak.Platform.Persistence.PostgreSql;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPostgreSqlPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<PlatformDbContext>(
            options =>
            {
                options.UseNpgsql(
                    connectionString,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay:
                                TimeSpan.FromSeconds(5),
                            errorCodesToAdd: null);
                    });
            });

        services.AddScoped<
            IExternalIdentityResolver,
            PostgreSqlExternalIdentityResolver>();

        services.AddScoped<
            IPhoneVerificationChallengeRepository,
            PostgreSqlPhoneVerificationChallengeRepository>();

        services.AddScoped<
            IPhoneAccountRepository,
            PostgreSqlPhoneAccountRepository>();

        services.AddScoped<
            IIdentityUnitOfWork,
            PostgreSqlIdentityUnitOfWork>();

        services.AddScoped<
            IUserSessionRepository,
            PostgreSqlUserSessionRepository>();

        services.AddScoped<
            IMarketListingRepository,
            PostgreSqlMarketListingRepository>();

        services.AddScoped<
            IPublishedListingQuery,
            PostgreSqlPublishedListingQuery>();

        services.AddScoped<
            IPropertyExistenceChecker,
            PostgreSqlPropertyExistenceChecker>();

        services.AddScoped<
            IPropertyListingAuthorizationChecker,
            PostgreSqlPropertyListingAuthorizationChecker>();

        services.AddScoped<
            IMarketPropertyRepository,
            PostgreSqlMarketPropertyRepository>();

        services.AddScoped<
            IPublisherPhoneLookup,
            PostgreSqlPublisherPhoneLookup>();

        services.AddScoped<
            IListingPhotoRepository,
            PostgreSqlListingPhotoRepository>();

        services.AddScoped<PostgreSqlListingReportRepository>();
        services.AddScoped<IListingReportRepository>(
            provider => provider.GetRequiredService<PostgreSqlListingReportRepository>());
        services.AddScoped<IListingModerationQuery>(
            provider => provider.GetRequiredService<PostgreSqlListingReportRepository>());

        services.AddScoped<
            Diyarak.Market.Application.Favorites.IListingFavoriteStore,
            PostgreSqlListingFavoriteStore>();

        services.AddScoped<
            Diyarak.Market.Application.SavedSearches.ISavedSearchStore,
            PostgreSqlSavedSearchStore>();

        services.AddScoped<
            Diyarak.Market.Application.Statistics.IListingStatisticsStore,
            PostgreSqlListingStatisticsStore>();

        services.AddScoped<
            Diyarak.Market.Application.Agencies.IAgencyRepository,
            PostgreSqlAgencyRepository>();

        services.AddScoped<
            Diyarak.Market.Application.Agencies.IAgencyPackageRepository,
            PostgreSqlAgencyPackageRepository>();

        services.AddScoped<
            Diyarak.Market.Application.Agencies.IActiveListingCounter,
            PostgreSqlActiveListingCounter>();

        services.AddScoped<
            Diyarak.Market.Application.OffPlanProjects.IOffPlanProjectRepository,
            PostgreSqlOffPlanProjectRepository>();

        services.AddScoped<
            IAreaPriceReportQuery,
            PostgreSqlAreaPriceReportQuery>();

        services.AddScoped<
            Diyarak.Market.Application.Contractors.IContractorRepository,
            PostgreSqlContractorRepository>();

        services.AddScoped<
            IReviewRepository,
            PostgreSqlReviewRepository>();

        services.AddScoped<
            Diyarak.Market.Application.Payments.IPaymentOrderStore,
            PostgreSqlPaymentOrderStore>();

        services.AddScoped<
            Diyarak.Market.Application.Payments.IListingPromotionStore,
            PostgreSqlListingPromotionStore>();

        services.AddScoped<
            IDraftListingDeletionStore,
            PostgreSqlDraftListingDeletionStore>();

        services.AddScoped<
            IMarketTransactionRunner,
            PostgreSqlMarketTransactionRunner>();

        return services;
    }
}
