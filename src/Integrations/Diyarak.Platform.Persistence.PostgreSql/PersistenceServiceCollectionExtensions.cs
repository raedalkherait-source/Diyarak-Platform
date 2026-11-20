using Diyarak.Market.Application;
using Diyarak.Market.Application.Photos;
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

        services.AddScoped<
            IMarketTransactionRunner,
            PostgreSqlMarketTransactionRunner>();

        return services;
    }
}
