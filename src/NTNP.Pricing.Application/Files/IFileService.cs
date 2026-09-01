using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Contracts.Files;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Application.Files;

/// <summary>Section 32 — server-controlled stored file metadata (list/download + registration).</summary>
public interface IFileService
{
    Task<IReadOnlyList<StoredFileDto>> ListAsync(Guid? projectId, Guid? projectRevisionId, string? category, CancellationToken ct = default);

    /// <summary>Returns the metadata row; the caller opens the actual content via <see cref="IFileStorageService"/>.</summary>
    Task<StoredFileDto> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Resolves the server-disk storage path for <see cref="IFileStorageService.OpenReadAsync"/>.
    /// Deliberately separate from <see cref="StoredFileDto"/>, which never exposes the raw path to
    /// clients — only the Api layer's download endpoint calls this.
    /// </summary>
    Task<string> GetStoragePathAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Registers metadata for a file already written by <see cref="IFileStorageService"/> — used by
    /// report generation (Section 26/16/19/21) and Excel import (Section 8) so every server-produced
    /// document has a durable <see cref="Domain.Entities.StoredFile"/> row (Section 32).
    /// </summary>
    Task<StoredFileDto> RegisterAsync(
        StoredFileResult savedFile, string fileName, string contentType, FileCategory category,
        Guid? projectId, Guid? projectRevisionId, CancellationToken ct = default);
}
