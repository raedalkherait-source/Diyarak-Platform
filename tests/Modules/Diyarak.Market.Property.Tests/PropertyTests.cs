using Diyarak.Platform.Domain.Primitives;
using Xunit;

namespace Diyarak.Market.Property.Tests;

public sealed class PropertyTests
{
    [Fact]
    public void Constructor_sets_id_category_and_address()
    {
        Guid id = Guid.NewGuid();
        var address = CreateAddress();

        var property = new Property(id, PropertyCategory.Apartment, address);

        Assert.Equal(id, property.Id);
        Assert.Equal(PropertyCategory.Apartment, property.Category);
        Assert.Equal(address, property.Address);
        Assert.Null(property.LivingArea);
        Assert.Null(property.UsableArea);
        Assert.Null(property.TotalRooms);
        Assert.Null(property.BedroomCount);
        Assert.Null(property.BathroomCount);
        Assert.Null(property.FurnishingQuality);
        Assert.Empty(property.Features);
    }

    [Fact]
    public void Constructor_sets_optional_areas()
    {
        var livingArea = new Area(120m, AreaUnit.SquareMeter);
        var usableArea = new Area(145m, AreaUnit.SquareMeter);

        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.House,
            CreateAddress(),
            livingArea,
            usableArea);

        Assert.Equal(livingArea, property.LivingArea);
        Assert.Equal(usableArea, property.UsableArea);
    }

    [Fact]
    public void Constructor_sets_optional_room_counts()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            CreateAddress(),
            totalRooms: 4m,
            bedroomCount: 3,
            bathroomCount: 2);

        Assert.Equal(4m, property.TotalRooms);
        Assert.Equal(3, property.BedroomCount);
        Assert.Equal(2, property.BathroomCount);
    }

    [Fact]
    public void Constructor_sets_optional_furnishing_quality()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            CreateAddress(),
            furnishingQuality: FurnishingQuality.Luxury);

        Assert.Equal(FurnishingQuality.Luxury, property.FurnishingQuality);
    }

    [Fact]
    public void Constructor_sets_distinct_optional_features()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            CreateAddress(),
            features:
            [
                PropertyFeature.Elevator,
                PropertyFeature.BalconyOrTerrace,
                PropertyFeature.Elevator
            ]);

        Assert.Equal(2, property.Features.Count);
        Assert.Contains(PropertyFeature.Elevator, property.Features);
        Assert.Contains(PropertyFeature.BalconyOrTerrace, property.Features);
    }

    [Fact]
    public void Constructor_sets_optional_management_owner()
    {
        Guid ownerUserId = Guid.NewGuid();

        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            CreateAddress(),
            ownerUserId: ownerUserId);

        Assert.Equal(ownerUserId, property.OwnerUserId);
    }

    [Fact]
    public void Constructor_allows_unowned_legacy_property_state()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            CreateAddress());

        Assert.Null(property.OwnerUserId);
    }

    [Fact]
    public void Constructor_rejects_empty_management_owner_identifier()
    {
        Assert.Throws<ArgumentException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.Apartment,
                CreateAddress(),
                ownerUserId: Guid.Empty));
    }

    [Fact]
    public void Constructor_rejects_empty_id()
    {
        Assert.Throws<ArgumentException>(
            () => new Property(Guid.Empty, PropertyCategory.House, CreateAddress()));
    }

    [Fact]
    public void Constructor_rejects_invalid_category()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(Guid.NewGuid(), (PropertyCategory)0, CreateAddress()));
    }

    [Fact]
    public void Constructor_rejects_null_address()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Property(Guid.NewGuid(), PropertyCategory.Apartment, null!));
    }

    [Fact]
    public void Constructor_rejects_negative_room_counts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.Apartment,
                CreateAddress(),
                totalRooms: -1m));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.Apartment,
                CreateAddress(),
                bedroomCount: -1));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.Apartment,
                CreateAddress(),
                bathroomCount: -1));
    }

    [Fact]
    public void Constructor_rejects_invalid_furnishing_quality()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.Apartment,
                CreateAddress(),
                furnishingQuality: (FurnishingQuality)0));
    }

    [Fact]
    public void Constructor_rejects_invalid_feature()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Property(
                Guid.NewGuid(),
                PropertyCategory.Apartment,
                CreateAddress(),
                features: [(PropertyFeature)0]));
    }

    [Fact]
    public void Constructor_starts_at_initial_version()
    {
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            CreateAddress());

        Assert.Equal(Property.InitialVersion, property.Version);
    }

    [Fact]
    public void Restore_preserves_persisted_version()
    {
        var property = Property.Restore(
            Guid.NewGuid(),
            PropertyCategory.Apartment,
            CreateAddress(),
            livingArea: null,
            usableArea: null,
            totalRooms: null,
            bedroomCount: null,
            bathroomCount: null,
            furnishingQuality: null,
            features: null,
            constructionYear: null,
            lastModernizationYear: null,
            commercialSubtype: null,
            salesArea: null,
            totalArea: null,
            parkingSpaceCount: null,
            ownerUserId: Guid.NewGuid(),
            version: 7);

        Assert.Equal(7, property.Version);
    }

    [Fact]
    public void ReplaceDetails_changes_mutable_state_without_changing_owner_or_version()
    {
        Guid ownerUserId = Guid.NewGuid();
        var property = new Property(
            Guid.NewGuid(),
            PropertyCategory.House,
            CreateAddress(),
            ownerUserId: ownerUserId);

        property.ReplaceDetails(
            PropertyCategory.Apartment,
            new PropertyAddress("New Street", "9", "54321", "New City"),
            livingArea: new Area(90m, AreaUnit.SquareMeter));

        Assert.Equal(ownerUserId, property.OwnerUserId);
        Assert.Equal(Property.InitialVersion, property.Version);
        Assert.Equal(PropertyCategory.Apartment, property.Category);
        Assert.Equal("New Street", property.Address.Street);
        Assert.Equal(90m, property.LivingArea?.Value);
    }

    private static PropertyAddress CreateAddress() =>
        new("Example Street", "12A", "12345", "Example City");
}
