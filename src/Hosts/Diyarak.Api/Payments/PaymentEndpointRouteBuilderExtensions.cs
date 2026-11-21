using Diyarak.Api.Authentication;
using Diyarak.Api.Market;
using Diyarak.Api.Moderation;
using Diyarak.Market.Application;
using Diyarak.Market.Application.Payments;
using Diyarak.Platform.BuildingBlocks;
using MarketListing = Diyarak.Market.Listing.Listing;

namespace Diyarak.Api.Payments;

/// <summary>
/// Section <c>Payments</c>: prices per product and instructions per local payment method. Products
/// or methods without configuration are not offered (ADR-0071).
/// </summary>
internal sealed class ConfiguredPaymentCatalog : IPaymentCatalog
{
    private readonly Dictionary<PaymentProduct, PaymentPrice> _prices = [];
    private readonly Dictionary<PaymentMethod, string> _instructions = [];

    public ConfiguredPaymentCatalog(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("Payments");

        foreach (PaymentProduct product in Enum.GetValues<PaymentProduct>())
        {
            IConfigurationSection price = section.GetSection($"Products:{product}");
            decimal? amount = price.GetValue<decimal?>("Amount");
            string? currency = price.GetValue<string?>("Currency");
            if (amount is > 0 && currency is { Length: 3 })
                _prices[product] = new PaymentPrice(amount.Value, currency.ToUpperInvariant());
        }

        foreach (PaymentMethod method in Enum.GetValues<PaymentMethod>())
        {
            string? instructions = section.GetValue<string?>($"Methods:{method}");
            if (!string.IsNullOrWhiteSpace(instructions))
                _instructions[method] = instructions.Trim();
        }
    }

    public PaymentPrice? FindPrice(PaymentProduct product) => _prices.GetValueOrDefault(product);

    public string? FindInstructions(PaymentMethod method) => _instructions.GetValueOrDefault(method);

    internal IEnumerable<(PaymentProduct Product, PaymentPrice Price)> Products => _prices.Select(pair => (pair.Key, pair.Value));

    internal IEnumerable<(PaymentMethod Method, string Instructions)> Methods => _instructions.Select(pair => (pair.Key, pair.Value));
}

internal static class PaymentEndpointRouteBuilderExtensions
{
    internal static IServiceCollection AddPayments(this IServiceCollection services)
    {
        services.AddSingleton<ConfiguredPaymentCatalog>();
        services.AddSingleton<IPaymentCatalog>(provider => provider.GetRequiredService<ConfiguredPaymentCatalog>());
        services.AddScoped<CreatePaymentOrderUseCase>();
        services.AddScoped<DecidePaymentOrderUseCase>();
        return services;
    }

    internal static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/market/payments/catalog", Catalog).RequireMappedUserManagement();
        endpoints.MapGet("/api/market/payments/orders", MyOrdersAsync).RequireMappedUserManagement();
        endpoints.MapPost("/api/market/payments/orders", CreateAsync).RequireMappedUserManagement();
        endpoints.MapGet("/api/market/listings/{listingId}/promotion", PromotionAsync).RequireMappedUserManagement();

        RouteGroupBuilder moderation = endpoints.MapGroup("/api/moderation/payments");
        moderation.RequireAuthorization(ModerationEndpointRouteBuilderExtensions.ModeratorPolicy);
        moderation.WithMetadata(MarketManagementEndpointMetadata.Instance);
        moderation.MapGet(string.Empty, PendingAsync);
        moderation.MapPost("/{orderId}/confirm", (string orderId, IAuthenticatedActorAccessor actor, DecidePaymentOrderUseCase useCase, HttpContext http, CancellationToken token) =>
            DecideAsync(orderId, PaymentDecision.Confirm, null, actor, useCase, http, token));
        moderation.MapPost("/{orderId}/reject", (string orderId, RejectRequest? request, IAuthenticatedActorAccessor actor, DecidePaymentOrderUseCase useCase, HttpContext http, CancellationToken token) =>
            DecideAsync(orderId, PaymentDecision.Reject, request?.Note, actor, useCase, http, token));

        return endpoints;
    }

    internal static IResult Catalog(ConfiguredPaymentCatalog catalog, HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new CatalogResponse(
            [.. catalog.Products.OrderBy(item => item.Product).Select(item => new ProductResponse(item.Product.ToString(), PaymentProducts.FeaturedDays(item.Product), item.Price.Amount, item.Price.Currency))],
            [.. catalog.Methods.OrderBy(item => item.Method).Select(item => new MethodResponse(item.Method.ToString(), item.Instructions))]));
    }

    internal static async Task<IResult> MyOrdersAsync(IAuthenticatedActorAccessor actor, IPaymentOrderStore orders, HttpContext http, CancellationToken token)
    {
        http.Response.Headers.CacheControl = "no-store";
        IReadOnlyList<PaymentOrder> mine = await orders.ListByUserAsync(RequireActor(actor), 50, token);
        return Results.Ok(new OrderListResponse([.. mine.Select(ToResponse)]));
    }

    internal static async Task<IResult> CreateAsync(CreateOrderRequest? request, IAuthenticatedActorAccessor actor, CreatePaymentOrderUseCase useCase, HttpContext http, CancellationToken token)
    {
        if (request is null ||
            !Guid.TryParse(request.ListingId, out Guid listingId) ||
            !Enum.TryParse(request.Product, ignoreCase: false, out PaymentProduct product) ||
            !Enum.TryParse(request.Method, ignoreCase: false, out PaymentMethod method) ||
            int.TryParse(request.Product, out _) || int.TryParse(request.Method, out _))
        {
            return ToProblem(PaymentErrors.InvalidRequest, http);
        }

        Result<PaymentOrder> result = await useCase.ExecuteAsync(RequireActor(actor), listingId, product, method, request.Reference ?? string.Empty, token);
        return result.IsSuccess
            ? Results.Created($"/api/market/payments/orders/{result.Value.Id}", ToResponse(result.Value))
            : ToProblem(result.Error, http);
    }

    internal static async Task<IResult> PromotionAsync(string listingId, IAuthenticatedActorAccessor actor, IMarketListingRepository listings, IListingPromotionStore promotions, IClock clock, HttpContext http, CancellationToken token)
    {
        if (!Guid.TryParse(listingId, out Guid parsed))
            return ToProblem(PaymentErrors.ListingNotFound, http);

        MarketListing? listing = await listings.FindByIdAsync(parsed, token);
        if (listing is null || listing.PublisherUserId != RequireActor(actor))
            return ToProblem(PaymentErrors.ListingNotFound, http);

        http.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new PromotionResponse(parsed, await promotions.FindActiveUntilAsync(parsed, clock.UtcNow, token)));
    }

    internal static async Task<IResult> PendingAsync(IPaymentOrderStore orders, HttpContext http, CancellationToken token)
    {
        http.Response.Headers.CacheControl = "no-store";
        IReadOnlyList<PaymentOrder> pending = await orders.ListByStatusAsync(PaymentOrderStatus.Pending, 100, token);
        return Results.Ok(new OrderListResponse([.. pending.Select(ToResponse)]));
    }

    internal static async Task<IResult> DecideAsync(string orderId, PaymentDecision decision, string? note, IAuthenticatedActorAccessor actor, DecidePaymentOrderUseCase useCase, HttpContext http, CancellationToken token)
    {
        if (!Guid.TryParse(orderId, out Guid parsed))
            return ToProblem(PaymentErrors.NotFound, http);

        Result<PaymentOrder> result = await useCase.ExecuteAsync(parsed, RequireActor(actor), decision, note, token);
        return result.IsSuccess ? Results.Ok(ToResponse(result.Value)) : ToProblem(result.Error, http);
    }

    private static OrderResponse ToResponse(PaymentOrder order) =>
        new(order.Id, order.ListingId, order.Product.ToString(), PaymentProducts.FeaturedDays(order.Product), order.Price.Amount, order.Price.Currency,
            order.Method.ToString(), order.PayerReference, order.Status.ToString(), order.DecisionNote, order.CreatedAt, order.DecidedAt);

    private static Guid RequireActor(IAuthenticatedActorAccessor actor) =>
        actor.UserId is { } userId && userId != Guid.Empty
            ? userId
            : throw new InvalidOperationException("An authorization policy succeeded without an internal user identifier.");

    private static IResult ToProblem(Error error, HttpContext http)
    {
        (int status, string title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Bad Request"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Not Found"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
            _ => throw new InvalidOperationException($"Unsupported expected payment error type '{error.Type}'."),
        };
        return Results.Problem(statusCode: status, title: title, detail: error.Description,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code, ["traceId"] = http.TraceIdentifier });
    }

    internal sealed record CreateOrderRequest(string? ListingId, string? Product, string? Method, string? Reference);
    internal sealed record RejectRequest(string? Note);
    internal sealed record ProductResponse(string Product, int Days, decimal Amount, string Currency);
    internal sealed record MethodResponse(string Method, string Instructions);
    internal sealed record CatalogResponse(IReadOnlyList<ProductResponse> Products, IReadOnlyList<MethodResponse> Methods);
    internal sealed record OrderResponse(Guid OrderId, Guid ListingId, string Product, int Days, decimal Amount, string Currency, string Method, string Reference, string Status, string? DecisionNote, DateTimeOffset CreatedAt, DateTimeOffset? DecidedAt);
    internal sealed record OrderListResponse(IReadOnlyList<OrderResponse> Items);
    internal sealed record PromotionResponse(Guid ListingId, DateTimeOffset? FeaturedUntil);
}
