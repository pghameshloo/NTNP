using NTNP.Pricing.Contracts.Equipment;

namespace NTNP.Pricing.Application.Equipment;

/// <summary>
/// Section 9's 10-step Excel import workflow (select file → map columns → preview → validate →
/// duplicate detection → row errors → insert/update summary → confirm → transactional commit →
/// import log). Implemented in Infrastructure (ClosedXML) since Section 5 assigns "imports" to
/// Infrastructure; Application only sees this interface.
/// </summary>
public interface IEquipmentImportService
{
    /// <summary>Parses the workbook, validates every row, and returns a preview keyed by a short-lived import token.</summary>
    Task<EquipmentImportPreviewResult> PreviewAsync(Stream fileContent, string fileName, CancellationToken ct = default);

    /// <summary>Commits a previously previewed import in one transaction; requires explicit confirmation (the token).</summary>
    Task<EquipmentImportCommitResult> CommitAsync(EquipmentImportCommitRequest request, CancellationToken ct = default);
}
