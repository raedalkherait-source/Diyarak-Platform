using System.Text.RegularExpressions;
using Diyarak.Market.Application.Photos;

namespace Diyarak.Platform.Storage.FileSystem;

/// <summary>
/// Stores photos under a root directory. Suitable for development and single-node deployments;
/// multi-node production should use S3-compatible object storage behind the same port.
/// </summary>
public sealed partial class FileSystemListingPhotoStorage : IListingPhotoStorage
{
    private readonly string _root;

    public FileSystemListingPhotoStorage(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        _root = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(
        string key,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        string path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Write to a temporary file first so readers never observe a partial photo.
        string temporary = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            await File.WriteAllBytesAsync(temporary, content.ToArray(), cancellationToken);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    public Task<Stream?> OpenReadAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        string path = ResolvePath(key);

        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true)
            : null;

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        string path = ResolvePath(key);

        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    internal string ResolvePath(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!KeyPattern().IsMatch(key) || key.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("Storage key contains unsupported characters.", nameof(key));

        string path = Path.GetFullPath(Path.Combine(_root, key));

        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Storage key resolves outside the storage root.", nameof(key));

        return path;
    }

    [GeneratedRegex("^[a-z0-9]+(/[a-z0-9]+)*\\.[a-z0-9]+$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();
}
