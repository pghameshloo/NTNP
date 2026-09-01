namespace NTNP.Pricing.Application.Common;

public sealed record StoredFileResult(string StoragePath, long SizeBytes, string Sha256Hash);

/// <summary>
/// Section 32/33 — server-controlled file storage abstraction. See ASSUMPTIONS.md §8: the default
/// implementation writes to a configurable directory on the application server; it is abstracted so
/// a UNC share or blob store can be substituted later without touching callers.
/// </summary>
public interface IFileStorageService
{
    Task<StoredFileResult> SaveAsync(string suggestedFileName, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
