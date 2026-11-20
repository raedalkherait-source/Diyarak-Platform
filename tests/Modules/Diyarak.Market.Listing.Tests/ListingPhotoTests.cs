using Xunit;

namespace Diyarak.Market.Listing.Tests;

public sealed class ListingPhotoTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_sets_metadata()
    {
        Guid id = Guid.NewGuid();
        Guid listingId = Guid.NewGuid();
        string key = ListingPhoto.CreateStorageKey(listingId, id, ListingPhotoFormat.Webp);

        var photo = new ListingPhoto(id, listingId, key, ListingPhotoFormat.Webp, 1234, 2, Now);

        Assert.Equal(listingId, photo.ListingId);
        Assert.Equal($"listings/{listingId:N}/{id:N}.webp", photo.StorageKey);
        Assert.Equal(ListingPhotoFormat.Webp, photo.Format);
        Assert.Equal(1234, photo.SizeBytes);
        Assert.Equal(2, photo.Position);
        Assert.Equal(Now, photo.UploadedAt);
    }

    [Fact]
    public void Constructor_rejects_invalid_values()
    {
        Guid id = Guid.NewGuid();
        Guid listingId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => new ListingPhoto(Guid.Empty, listingId, "k", ListingPhotoFormat.Jpeg, 1, 0, Now));
        Assert.Throws<ArgumentException>(() => new ListingPhoto(id, Guid.Empty, "k", ListingPhotoFormat.Jpeg, 1, 0, Now));
        Assert.Throws<ArgumentException>(() => new ListingPhoto(id, listingId, " ", ListingPhotoFormat.Jpeg, 1, 0, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ListingPhoto(id, listingId, "k", (ListingPhotoFormat)9, 1, 0, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ListingPhoto(id, listingId, "k", ListingPhotoFormat.Jpeg, 0, 0, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ListingPhoto(id, listingId, "k", ListingPhotoFormat.Jpeg, ListingPhoto.MaximumSizeBytes + 1, 0, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ListingPhoto(id, listingId, "k", ListingPhotoFormat.Jpeg, 1, -1, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ListingPhoto(id, listingId, "k", ListingPhotoFormat.Jpeg, 1, ListingPhoto.MaximumPhotosPerListing, Now));
    }

    [Theory]
    [InlineData(ListingPhotoFormat.Jpeg, "image/jpeg", "jpg")]
    [InlineData(ListingPhotoFormat.Png, "image/png", "png")]
    [InlineData(ListingPhotoFormat.Webp, "image/webp", "webp")]
    public void Format_maps_to_content_type_and_extension(ListingPhotoFormat format, string contentType, string extension)
    {
        Assert.Equal(contentType, format.ToContentType());
        Assert.Equal(extension, format.ToFileExtension());
    }
}
