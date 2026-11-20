using System.Threading.RateLimiting;

namespace Diyarak.Api.RateLimiting;

internal static class ClientRateLimitingServiceCollectionExtensions
{
    internal const string SectionName = "RateLimiting";

    internal const int DefaultPermitLimit = 100;

    internal const int DefaultWindowSeconds = 60;

    /// <summary>Stricter per-client policy for endpoints that send paid OTP messages.</summary>
    internal const string OtpPolicy = "otp";

    internal const int DefaultOtpPermitLimit = 10;

    internal const int DefaultOtpWindowSeconds = 900;

    /// <summary>Per-client policy for revealing publisher phone numbers, limiting scraping.</summary>
    internal const string ContactPolicy = "contact";

    internal const int DefaultContactPermitLimit = 30;

    internal const int DefaultContactWindowSeconds = 900;

    /// <summary>Per-client policy for anonymous listing reports.</summary>
    internal const string ReportPolicy = "report";

    /// <summary>
    /// Registers a global fixed-window limiter partitioned per client IP address so that a
    /// single client cannot exhaust capacity for everyone else. Health probes are exempt.
    /// </summary>
    /// <remarks>
    /// When the API runs behind a reverse proxy, forwarded headers must be configured for
    /// trusted proxies only; otherwise every request is attributed to the proxy address.
    /// </remarks>
    internal static IServiceCollection AddClientRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection section = configuration.GetSection(SectionName);

        int permitLimit =
            section.GetValue<int?>("PermitLimit") ?? DefaultPermitLimit;
        int windowSeconds =
            section.GetValue<int?>("WindowSeconds") ?? DefaultWindowSeconds;

        if (permitLimit <= 0)
        {
            throw new InvalidOperationException(
                $"'{SectionName}:PermitLimit' must be positive.");
        }

        if (windowSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"'{SectionName}:WindowSeconds' must be positive.");
        }

        var window = TimeSpan.FromSeconds(windowSeconds);

        int otpPermitLimit =
            section.GetValue<int?>("Otp:PermitLimit") ?? DefaultOtpPermitLimit;
        int otpWindowSeconds =
            section.GetValue<int?>("Otp:WindowSeconds") ?? DefaultOtpWindowSeconds;

        if (otpPermitLimit <= 0 || otpWindowSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"'{SectionName}:Otp' limits must be positive.");
        }

        var otpWindow = TimeSpan.FromSeconds(otpWindowSeconds);

        int contactPermitLimit =
            section.GetValue<int?>("Contact:PermitLimit") ?? DefaultContactPermitLimit;
        int contactWindowSeconds =
            section.GetValue<int?>("Contact:WindowSeconds") ?? DefaultContactWindowSeconds;

        if (contactPermitLimit <= 0 || contactWindowSeconds <= 0)
        {
            throw new InvalidOperationException(
                $"'{SectionName}:Contact' limits must be positive.");
        }

        var contactWindow = TimeSpan.FromSeconds(contactWindowSeconds);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter,
                        out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds))
                        .ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };

            options.AddPolicy(
                OtpPolicy,
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = otpPermitLimit,
                        Window = otpWindow,
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));

            options.AddPolicy(
                ContactPolicy,
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = contactPermitLimit,
                        Window = contactWindow,
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));

            options.AddPolicy(
                ReportPolicy,
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = section.GetValue<int?>("Report:PermitLimit") ?? 10,
                        Window = TimeSpan.FromSeconds(section.GetValue<int?>("Report:WindowSeconds") ?? 3600),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));

            options.GlobalLimiter =
                PartitionedRateLimiter.Create<HttpContext, string>(
                    httpContext =>
                    {
                        if (httpContext.Request.Path.StartsWithSegments("/health"))
                        {
                            return RateLimitPartition.GetNoLimiter("health");
                        }

                        string partitionKey =
                            httpContext.Connection.RemoteIpAddress?.ToString()
                            ?? "unknown";

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey,
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = permitLimit,
                                Window = window,
                                QueueLimit = 0,
                                AutoReplenishment = true,
                            });
                    });
        });

        return services;
    }
}
