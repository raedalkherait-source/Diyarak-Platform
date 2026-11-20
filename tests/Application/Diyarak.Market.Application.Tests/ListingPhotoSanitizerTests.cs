using System.Text;
using Diyarak.Market.Application.Photos;
using Diyarak.Market.Listing;
using Xunit;

namespace Diyarak.Market.Application.Tests;

public sealed class ListingPhotoSanitizerTests
{
    [Fact]
    public void Jpeg_metadata_is_removed_but_orientation_is_kept()
    {
        byte[] original = ReadFixture("exif-gps.jpg");
        Assert.True(Contains(original, "DiyarakTestCam"));
        Assert.True(Contains(original, "secret-comment"));

        ListingPhotoSanitizeStatus status =
            ListingPhotoSanitizer.Sanitize(original, out ListingPhotoFormat format, out byte[] sanitized);

        Assert.Equal(ListingPhotoSanitizeStatus.Sanitized, status);
        Assert.Equal(ListingPhotoFormat.Jpeg, format);
        Assert.False(Contains(sanitized, "DiyarakTestCam"));
        Assert.False(Contains(sanitized, "secret-comment"));
        Assert.Equal((ushort)6, ReadJpegOrientation(sanitized));
        Assert.Equal(0xFF, sanitized[0]);
        Assert.Equal(0xD8, sanitized[1]);
        Assert.Equal(original[^2..], sanitized[^2..]);
        Assert.True(sanitized.Length < original.Length);
    }

    [Fact]
    public void Jpeg_without_orientation_gets_no_exif_segment()
    {
        byte[] original = ReadFixture("exif-gps.jpg");
        ListingPhotoSanitizer.Sanitize(original, out _, out byte[] once);

        // Remove the preserved orientation by sanitizing a copy whose orientation value is 1.
        byte[] upright = (byte[])once.Clone();
        int valueOffset = FindOrientationValueOffset(upright);
        upright[valueOffset] = 0;
        upright[valueOffset + 1] = 1;

        ListingPhotoSanitizer.Sanitize(upright, out _, out byte[] sanitized);

        Assert.Null(ReadJpegOrientation(sanitized));
        Assert.False(Contains(sanitized, "Exif"));
    }

    [Fact]
    public void Png_text_and_exif_chunks_are_removed()
    {
        byte[] original = ReadFixture("text-exif.png");
        Assert.True(Contains(original, "secret-png-text"));

        ListingPhotoSanitizeStatus status =
            ListingPhotoSanitizer.Sanitize(original, out ListingPhotoFormat format, out byte[] sanitized);

        Assert.Equal(ListingPhotoSanitizeStatus.Sanitized, status);
        Assert.Equal(ListingPhotoFormat.Png, format);
        Assert.False(Contains(sanitized, "secret-png-text"));
        Assert.False(Contains(sanitized, "secret-png-itxt"));
        Assert.False(Contains(sanitized, "eXIf"));
        Assert.False(Contains(sanitized, "DiyarakTestCam"));
        Assert.True(Contains(sanitized, "IHDR"));
        Assert.True(Contains(sanitized, "IDAT"));
        Assert.True(Contains(sanitized, "IEND"));
    }

    [Fact]
    public void Webp_exif_and_xmp_chunks_are_removed_and_header_updated()
    {
        byte[] original = ReadFixture("exif-xmp.webp");
        Assert.True(Contains(original, "secret-xmp"));

        ListingPhotoSanitizeStatus status =
            ListingPhotoSanitizer.Sanitize(original, out ListingPhotoFormat format, out byte[] sanitized);

        Assert.Equal(ListingPhotoSanitizeStatus.Sanitized, status);
        Assert.Equal(ListingPhotoFormat.Webp, format);
        Assert.False(Contains(sanitized, "secret-xmp"));
        Assert.False(Contains(sanitized, "DiyarakTestCam"));
        Assert.Equal(
            sanitized.Length - 8,
            (int)BitConverter.ToUInt32(sanitized, 4));

        int vp8x = IndexOf(sanitized, "VP8X");
        if (vp8x >= 0)
            Assert.Equal(0, sanitized[vp8x + 8] & 0x0C);
    }

    [Theory]
    [InlineData("GIF89a-not-supported")]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg'/>")]
    [InlineData("")]
    public void Unsupported_content_is_rejected(string content)
    {
        Assert.Equal(
            ListingPhotoSanitizeStatus.UnsupportedFormat,
            ListingPhotoSanitizer.Sanitize(Encoding.ASCII.GetBytes(content), out _, out _));
    }

    [Theory]
    [InlineData("exif-gps.jpg")]
    [InlineData("text-exif.png")]
    [InlineData("exif-xmp.webp")]
    public void Truncated_images_are_rejected_as_malformed(string fixture)
    {
        byte[] original = ReadFixture(fixture);
        byte[] truncated = original[..Math.Min(original.Length, 40)];

        Assert.Equal(
            ListingPhotoSanitizeStatus.Malformed,
            ListingPhotoSanitizer.Sanitize(truncated, out _, out _));
    }

    [Fact]
    public void Jpeg_with_segment_length_past_end_is_malformed()
    {
        byte[] bytes = [0xFF, 0xD8, 0xFF, 0xE1, 0xFF, 0xFF, 0x00];

        Assert.Equal(
            ListingPhotoSanitizeStatus.Malformed,
            ListingPhotoSanitizer.Sanitize(bytes, out _, out _));
    }

    private static byte[] ReadFixture(string name) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    private static bool Contains(byte[] haystack, string needle) =>
        IndexOf(haystack, needle) >= 0;

    private static int IndexOf(byte[] haystack, string needle) =>
        haystack.AsSpan().IndexOf(Encoding.ASCII.GetBytes(needle));

    private static int FindOrientationValueOffset(byte[] jpeg)
    {
        int exif = IndexOf(jpeg, "Exif\0\0");
        Assert.True(exif >= 0);
        return exif + 6 + 18;
    }

    private static ushort? ReadJpegOrientation(byte[] jpeg)
    {
        int exif = IndexOf(jpeg, "Exif\0\0");
        return exif < 0
            ? null
            : ListingPhotoSanitizer.ReadExifOrientation(jpeg.AsSpan(exif + 6));
    }
}
