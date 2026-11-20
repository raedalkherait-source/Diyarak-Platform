using Xunit;

namespace Diyarak.Platform.Storage.FileSystem.Tests;

public sealed class FileSystemListingPhotoStorageTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "diyarak-storage-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Save_open_and_delete_round_trip()
    {
        var storage = new FileSystemListingPhotoStorage(_root);
        const string key = "listings/abc123/def456.jpg";

        await storage.SaveAsync(key, new byte[] { 1, 2, 3 }, "image/jpeg");

        await using (Stream? stream = await storage.OpenReadAsync(key))
        {
            Assert.NotNull(stream);
            using var copy = new MemoryStream();
            await stream!.CopyToAsync(copy);
            Assert.Equal([1, 2, 3], copy.ToArray());
        }

        Assert.Empty(Directory.GetFiles(Path.Combine(_root, "listings", "abc123"), "*.tmp"));

        await storage.DeleteAsync(key);
        Assert.Null(await storage.OpenReadAsync(key));

        // Deleting a missing object is a no-op.
        await storage.DeleteAsync(key);
    }

    [Fact]
    public async Task Save_overwrites_existing_object()
    {
        var storage = new FileSystemListingPhotoStorage(_root);
        const string key = "listings/a/b.png";

        await storage.SaveAsync(key, new byte[] { 1 }, "image/png");
        await storage.SaveAsync(key, new byte[] { 2, 2 }, "image/png");

        await using Stream? stream = await storage.OpenReadAsync(key);
        Assert.Equal(2, stream!.Length);
    }

    [Theory]
    [InlineData("../escape.jpg")]
    [InlineData("listings/../../escape.jpg")]
    [InlineData("/etc/passwd")]
    [InlineData("listings\\a\\b.jpg")]
    [InlineData("Listings/A/B.jpg")]
    [InlineData("listings/a/b")]
    [InlineData("listings//b.jpg")]
    [InlineData(" ")]
    public void Unsafe_keys_are_rejected(string key)
    {
        var storage = new FileSystemListingPhotoStorage(_root);

        Assert.ThrowsAny<ArgumentException>(() => storage.ResolvePath(key));
    }

    [Fact]
    public void Resolved_paths_stay_under_root()
    {
        var storage = new FileSystemListingPhotoStorage(_root);

        string path = storage.ResolvePath("listings/a1/b2.webp");

        Assert.StartsWith(Path.GetFullPath(_root) + Path.DirectorySeparatorChar, path, StringComparison.Ordinal);
    }
}
