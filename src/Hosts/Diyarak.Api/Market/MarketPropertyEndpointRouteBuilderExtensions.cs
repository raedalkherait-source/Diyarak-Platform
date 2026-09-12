using System.Text.Json;
using Diyarak.Market.Application;
using Diyarak.Platform.BuildingBlocks;

namespace Diyarak.Api.Market;

public static class MarketPropertyEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapMarketPropertyEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapPost(
                "/api/market/properties",
                CreatePropertyAsync)
            .RequireAuthorization(
                Diyarak.Api.Authentication.AuthenticationServiceCollectionExtensions.MappedUserPolicy);

        return endpoints;
    }

    internal static async Task<IResult> CreatePropertyAsync(
        JsonElement request,
        CreatePropertyUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!MarketPropertyCreateRequestParser.TryParse(
                request,
                out CreatePropertyCommand command))
        {
            return ToProblemDetails(
                CreatePropertyErrors.InvalidRequest,
                httpContext);
        }

        Result<Guid> result = await useCase.ExecuteAsync(
            command,
            cancellationToken);

        if (result.IsSuccess)
        {
            Guid propertyId = result.Value;

            return Results.Created(
                $"/api/market/properties/{propertyId}",
                new CreateMarketPropertyResponse(propertyId));
        }

        return ToProblemDetails(result.Error, httpContext);
    }

    private static IResult ToProblemDetails(
        Error error,
        HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(httpContext);

        if (error.Type != ErrorType.Validation)
        {
            throw new InvalidOperationException(
                $"Unsupported expected Market Property error type '{error.Type}'.");
        }

        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Bad Request",
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
                ["traceId"] = httpContext.TraceIdentifier,
            });
    }

    internal sealed record CreateMarketPropertyResponse(
        Guid PropertyId);
}
