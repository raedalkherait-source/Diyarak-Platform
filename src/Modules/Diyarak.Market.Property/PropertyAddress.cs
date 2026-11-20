using Diyarak.Platform.Domain.Primitives;
using Diyarak.Platform.SharedKernel;

namespace Diyarak.Market.Property;

/// <summary>
/// A property address. Only <see cref="City"/> is required: Syrian addresses are usually
/// expressed as governorate, city and district, and street numbers or postal codes are often
/// unavailable. Optional parts must be non-blank when supplied.
/// </summary>
public sealed class PropertyAddress : ValueObject
{
    public PropertyAddress(
        string? street,
        string? houseNumber,
        string? postalCode,
        string city,
        GeoCoordinate? location = null,
        SyrianGovernorate? governorate = null,
        string? district = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ThrowIfBlank(street, nameof(street));
        ThrowIfBlank(houseNumber, nameof(houseNumber));
        ThrowIfBlank(postalCode, nameof(postalCode));
        ThrowIfBlank(district, nameof(district));

        if (governorate is { } value && !Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(governorate),
                governorate,
                "Unsupported governorate.");
        }

        Street = street;
        HouseNumber = houseNumber;
        PostalCode = postalCode;
        City = city;
        Location = location;
        Governorate = governorate;
        District = district;
    }

    public SyrianGovernorate? Governorate { get; }

    public string City { get; }

    /// <summary>District or neighbourhood (الحي / المنطقة).</summary>
    public string? District { get; }

    public string? Street { get; }

    public string? HouseNumber { get; }

    public string? PostalCode { get; }

    public GeoCoordinate? Location { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Governorate;
        yield return City;
        yield return District;
        yield return Street;
        yield return HouseNumber;
        yield return PostalCode;
        yield return Location;
    }

    private static void ThrowIfBlank(string? value, string parameterName)
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Value cannot be empty or whitespace when supplied.",
                parameterName);
        }
    }
}
