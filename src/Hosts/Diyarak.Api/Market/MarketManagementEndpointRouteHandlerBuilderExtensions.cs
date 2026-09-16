
namespace Diyarak.Api.Market;

internal sealed class MarketManagementEndpointMetadata
{
    internal static MarketManagementEndpointMetadata Instance { get; } = new();

    private MarketManagementEndpointMetadata()
    {
    }
}

internal static class MarketManagementEndpointRouteHandlerBuilderExtensions
{
    internal static RouteHandlerBuilder RequireMappedUserManagement(
        this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.RequireAuthorization(
            Diyarak.Api.Authentication.AuthenticationServiceCollectionExtensions.MappedUserPolicy);
        builder.WithMetadata(MarketManagementEndpointMetadata.Instance);

        return builder;
    }
}

