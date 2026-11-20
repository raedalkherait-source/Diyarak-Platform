using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Diyarak.Api.RateLimiting;

internal static class TrustedProxyServiceCollectionExtensions
{
    internal const string SectionName = "ReverseProxy";

    /// <summary>
    /// Honours <c>X-Forwarded-For</c>/<c>X-Forwarded-Proto</c> only from explicitly configured
    /// proxies (<c>KnownProxies</c>, individual addresses) or networks (<c>KnownNetworks</c>, CIDR,
    /// for example a private container network). Per-client rate limits then see the real client
    /// address. With no configuration, forwarded headers are ignored.
    /// </summary>
    internal static bool AddTrustedProxies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string[] proxies =
            configuration.GetSection($"{SectionName}:KnownProxies").Get<string[]>() ?? [];
        string[] networks =
            configuration.GetSection($"{SectionName}:KnownNetworks").Get<string[]>() ?? [];

        if (proxies.Length == 0 && networks.Length == 0)
            return false;

        var parsedNetworks = new List<System.Net.IPNetwork>();

        foreach (string network in networks)
        {
            if (!System.Net.IPNetwork.TryParse(network, out System.Net.IPNetwork parsed))
            {
                throw new InvalidOperationException(
                    $"'{SectionName}:KnownNetworks' contains an invalid CIDR network '{network}'.");
            }

            if (parsed.PrefixLength == 0)
            {
                throw new InvalidOperationException(
                    $"'{SectionName}:KnownNetworks' must not trust every address.");
            }

            parsedNetworks.Add(parsed);
        }

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

                foreach (System.Net.IPNetwork network in parsedNetworks)
                    options.KnownIPNetworks.Add(network);
            });

        return true;
    }
}
