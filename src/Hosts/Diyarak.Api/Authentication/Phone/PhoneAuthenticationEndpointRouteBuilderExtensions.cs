using System.Text.Json;
using Diyarak.Api.RateLimiting;
using Diyarak.Platform.BuildingBlocks;
using Diyarak.Platform.Identity.Application;

namespace Diyarak.Api.Authentication.Phone;

internal static class PhoneAuthenticationEndpointRouteBuilderExtensions
{
    internal const string RequestPath = "/api/auth/phone/request";

    internal const string VerifyPath = "/api/auth/phone/verify";

    internal static IEndpointRouteBuilder MapPhoneAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints
            .MapPost(RequestPath, RequestCodeAsync)
            .AllowAnonymous()
            .RequireRateLimiting(ClientRateLimitingServiceCollectionExtensions.OtpPolicy);

        endpoints
            .MapPost(VerifyPath, VerifyCodeAsync)
            .AllowAnonymous()
            .RequireRateLimiting(ClientRateLimitingServiceCollectionExtensions.OtpPolicy);

        return endpoints;
    }

    internal static async Task<IResult> RequestCodeAsync(
        JsonElement request,
        RequestPhoneVerificationUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        httpContext.Response.Headers.CacheControl = "no-store";

        Result<DateTimeOffset> result = await useCase.ExecuteAsync(
            ReadString(request, "phoneNumber"),
            cancellationToken);

        return result.IsSuccess
            ? Results.Accepted(
                value: new PhoneCodeRequestedResponse(result.Value))
            : ToProblem(result.Error, httpContext);
    }

    internal static async Task<IResult> VerifyCodeAsync(
        JsonElement request,
        VerifyPhoneUseCase useCase,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        httpContext.Response.Headers.CacheControl = "no-store";

        Result<PhoneSignInResult> result = await useCase.ExecuteAsync(
            ReadString(request, "phoneNumber"),
            ReadString(request, "code"),
            cancellationToken);

        if (!result.IsSuccess)
            return ToProblem(result.Error, httpContext);

        PhoneSignInResult signIn = result.Value;

        return Results.Ok(
            new PhoneSignInResponse(
                signIn.AccessToken.Value,
                "Bearer",
                signIn.AccessToken.ExpiresAt,
                signIn.UserId,
                signIn.IsNewUser));
    }

    private static string? ReadString(JsonElement request, string propertyName) =>
        request.ValueKind == JsonValueKind.Object &&
        request.TryGetProperty(propertyName, out JsonElement value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static IResult ToProblem(Error error, HttpContext httpContext)
    {
        int statusCode =
            error == PhoneAuthenticationErrors.TooManyRequests
                ? StatusCodes.Status429TooManyRequests
                : error.Type switch
                {
                    ErrorType.Validation => StatusCodes.Status400BadRequest,
                    ErrorType.Conflict => StatusCodes.Status409Conflict,
                    ErrorType.Failure => StatusCodes.Status503ServiceUnavailable,
                    _ => throw new InvalidOperationException(
                        $"Unsupported phone authentication error type '{error.Type}'."),
                };

        return Results.Problem(
            statusCode: statusCode,
            detail: error.Description,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
                ["traceId"] = httpContext.TraceIdentifier,
            });
    }

    internal sealed record PhoneCodeRequestedResponse(
        DateTimeOffset ExpiresAt);

    internal sealed record PhoneSignInResponse(
        string AccessToken,
        string TokenType,
        DateTimeOffset ExpiresAt,
        Guid UserId,
        bool IsNewUser);
}
