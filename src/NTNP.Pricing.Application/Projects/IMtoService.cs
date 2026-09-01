using NTNP.Pricing.Contracts.Mto;

namespace NTNP.Pricing.Application.Projects;

public interface IMtoService
{
    /// <summary>Section 16 — Automatic Consolidated MTO Generator for one project revision.</summary>
    Task<MtoResultDto> GetMtoAsync(Guid revisionId, CancellationToken ct = default);
}
