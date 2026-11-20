namespace Diyarak.Market.Listing;

public enum ListingPhotoFormat
{
    Jpeg = 1,
    Png = 2,
    Webp = 3
}

public static class ListingPhotoFormatExtensions
{
    public static string ToContentType(this ListingPhotoFormat format) =>
        format switch
        {
            ListingPhotoFormat.Jpeg => "image/jpeg",
            ListingPhotoFormat.Png => "image/png",
            ListingPhotoFormat.Webp => "image/webp",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported photo format."),
        };

    public static string ToFileExtension(this ListingPhotoFormat format) =>
        format switch
        {
            ListingPhotoFormat.Jpeg => "jpg",
            ListingPhotoFormat.Png => "png",
            ListingPhotoFormat.Webp => "webp",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported photo format."),
        };
}
