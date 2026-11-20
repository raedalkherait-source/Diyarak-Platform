using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Diyarak.Api.RateLimiting;

internal static class TrustedProxyServiceCollectionExtensions
{
    internal const string SectionName = "ReverseProxy";

    /// <summary>
    /// Honours <c>X-Forwarded-For</c>/<c>X-Forwarded-Proto</c> only from explicitly configured
    /// proxies (for example the portal server or a load balancer), so per-client rate limits see
    /// the real client address. With no configuration, forwarded headers are ignored.
    /// </summary>
    internal static bool AddTrustedProxies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string[] proxies =
            configuration.GetSection($"{SectionName}:KnownProxies").Get<string[]>() ?? [];

        if (proxies.Length == 0)
            return false;

        var addresses = new List<IPAddress>();

        foreach (string proxy in proxies)
        {
            if (!IPAddress.TryParse(proxy, out IPAddress? address))
            {
                throw new InvalidOperationException(
                    $"'{SectionName}:KnownProxies' contains an invalid IP address '{proxy}'.");
            }

            addresses.Add(address);
        }

        services.Configure<ForwardedHeadersOptions>(
            options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 1;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();

                foreach (IPAddress address in addresses)
                    options.KnownProxies.Add(address);
            });

        return true;
    }
}
