using Diyarak.Platform.Domain.Primitives;

namespace Diyarak.Api.Market;

/// <summary>
/// The only form in which a property's coordinates leave the API publicly (ADR-0065): snapped to
/// a grid of <see cref="GridDegrees"/> (about 450 m north–south, 370 m east–west in Syria), so the
/// exact building cannot be recovered while the neighbourhood stays recognisable.
/// </summary>
internal sealed record PublicApproximateLocation(
    double Latitude,
    double Longitude,
    int RadiusMeters)
{
    internal const double GridDegrees = 0.004;

    internal const int DisplayRadiusMeters = 500;

    internal static PublicApproximateLocation? From(GeoCoordinate? location) =>
        location is { } value
            ? new PublicApproximateLocation(
                Snap(value.Latitude),
                Snap(value.Longitude),
                DisplayRadiusMeters)
            : null;

    private static double Snap(double value) =>
        Math.Round(Math.Round(value / GridDegrees, MidpointRounding.AwayFromZero) * GridDegrees, 3);
}
