namespace NTNP.Pricing.Contracts.Files;

public sealed record StoredFileDto(
    Guid Id, string FileName, string ContentType, string Category, Guid? ProjectId, Guid? ProjectRevisionId,
    long SizeBytes, string Sha256Hash, string CreatedByUserName, DateTimeOffset CreatedAtUtc);
