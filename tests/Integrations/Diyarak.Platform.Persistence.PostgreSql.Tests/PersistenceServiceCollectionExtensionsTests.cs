using Diyarak.Market.Application;
using Diyarak.Platform.Identity;
using Diyarak.Platform.Persistence.PostgreSql.Identity;
using Diyarak.Platform.Persistence.PostgreSql.Market;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Diyarak.Platform.Persistence.PostgreSql.Tests;

public sealed class PersistenceServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPostgreSqlPersistence_registers_application_ports()
    {
        var services = new ServiceCollection();

        services.AddPostgreSqlPersistence(
            "Host=localhost;Database=diyarak_registration_test");

        using ServiceProvider provider =
            services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();

        IExternalIdentityResolver resolver =
            scope.ServiceProvider
                .GetRequiredService<IExternalIdentityResolver>();

        IPropertyExistenceChecker checker =
            scope.ServiceProvider
                .GetRequiredService<IPropertyExistenceChecker>();

        IPropertyListingAuthorizationChecker
            listingAuthorizationChecker =
                scope.ServiceProvider.GetRequiredService<
                    IPropertyListingAuthorizationChecker>();

        IMarketPropertyRepository propertyRepository =
            scope.ServiceProvider
                .GetRequiredService<IMarketPropertyRepository>();

        IMarketListingRepository repository =
            scope.ServiceProvider
                .GetRequiredService<IMarketListingRepository>();

        IPublishedListingQuery publishedListingQuery =
            scope.ServiceProvider
                .GetRequiredService<IPublishedListingQuery>();

        IMarketTransactionRunner transactionRunner =
            scope.ServiceProvider
                .GetRequiredService<IMarketTransactionRunner>();

        Assert.IsType<PostgreSqlExternalIdentityResolver>(
            resolver);

        Assert.IsType<PostgreSqlPropertyExistenceChecker>(
            checker);

        Assert.IsType<PostgreSqlPropertyListingAuthorizationChecker>(
            listingAuthorizationChecker);

        Assert.IsType<PostgreSqlMarketPropertyRepository>(
            propertyRepository);

        Assert.IsType<PostgreSqlMarketListingRepository>(
            repository);

        Assert.IsType<PostgreSqlPublishedListingQuery>(
            publishedListingQuery);

        Assert.IsType<PostgreSqlMarketTransactionRunner>(
            transactionRunner);
    }
}
