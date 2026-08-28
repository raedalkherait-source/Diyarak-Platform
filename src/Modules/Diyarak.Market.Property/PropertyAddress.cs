using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Property;

public sealed class PropertyAddress : ValueObject
{
    public PropertyAddress(
        string street,
        string houseNumber,
        string postalCode,
        string city,
        GeoCoordinate? location = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(houseNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);

        Street = street;
        HouseNumber = houseNumber;
        PostalCode = postalCode;
        City = city;
        Location = location;
    }

    public string Street { get; }

    public string HouseNumber { get; }

    public string PostalCode { get; }

    public string City { get; }

    public GeoCoordinate? Location { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return HouseNumber;
        yield return PostalCode;
        yield return City;
        yield return Location;
    }
}
