namespace Diyarak.Platform.Domain.Primitives;

public readonly record struct GeoCoordinate
{
    public GeoCoordinate(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || double.IsInfinity(latitude) || latitude is < -90d or > 90d)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        if (double.IsNaN(longitude) || double.IsInfinity(longitude) || longitude is < -180d or > 180d)
            throw new ArgumentOutOfRangeException(nameof(longitude));
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }
    public double Longitude { get; }
    public override string ToString() => $"{Latitude:R},{Longitude:R}";
}
