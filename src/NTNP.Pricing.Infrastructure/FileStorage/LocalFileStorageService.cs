using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using NTNP.Pricing.Application.Common;

namespace NTNP.Pricing.Infrastructure.FileStorage;

public sealed class FileStorageOptions
{
    /// <summary>Section 32/33 — a directory on the application server's disk (or an attached UNC share).</summary>
    public string RootPath { get; set; } = "C:\\NTNP\\Pricing\\Storage";
}

/// <summary>
/// Section 32 — the default server-controlled file storage implementation (see ASSUMPTIONS.md §8).
/// Files are written under a category subfolder, named by a new GUID to avoid collisions/traversal;
/// the human-readable original filename is kept only in <see cref="Domain.Entities.StoredFile"/>.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;

    public LocalFileStorageService(IOptions<FileStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<StoredFileResult> SaveAsync(string suggestedFileName, Stream content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_options.RootPath);

        var extension = Path.GetExtension(suggestedFileName);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_options.RootPath, storedName);

        using var sha256 = SHA256.Create();
        await using (var fileStream = File.Create(fullPath))
        await using (var hashingStream = new CryptoStream(fileStream, sha256, CryptoStreamMode.Write, leaveOpen: false))
        {
            await content.CopyToAsync(hashingStream, cancellationToken);
        }

        var hash = Convert.ToHexString(sha256.Hash ?? Array.Empty<byte>()).ToLowerInvariant();
        var size = new FileInfo(fullPath).Length;

        return new StoredFileResult(fullPath, size, hash);
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        Stream stream = File.OpenRead(storagePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(storagePath))
        {
            File.Delete(storagePath);
        }
        return Task.CompletedTask;
    }
}
