using NTNP.Pricing.Domain.Common;
using NTNP.Pricing.Domain.Enums;

namespace NTNP.Pricing.Domain.Entities;

/// <summary>
/// Section 32 — metadata for a file held in server-controlled storage (see
/// ASSUMPTIONS.md §8 for the storage abstraction). The client never writes authoritative documents
/// only to local disk; every generated report/import is registered here.
/// </summary>
public class StoredFile : AuditableEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public FileCategory Category { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ProjectRevisionId { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
}
