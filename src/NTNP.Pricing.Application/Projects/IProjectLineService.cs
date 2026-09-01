using NTNP.Pricing.Contracts.Projects;

namespace NTNP.Pricing.Application.Projects;

public interface IProjectLineService
{
    /// <summary>Section 15 — Automatic BOM Generator: copies the template BOM/BODY+ES, resolves
    /// current equipment prices and purchase rates, and stores immutable snapshots on the new line.</summary>
    Task<ProjectRevisionDto> AddLineAsync(Guid revisionId, AddProjectLineRequest request, CancellationToken ct = default);

    Task<ProjectRevisionDto> UpdateLineQuantityAsync(Guid revisionId, Guid lineId, UpdateProjectLineQuantityRequest request, CancellationToken ct = default);
    Task<ProjectRevisionDto> RemoveLineAsync(Guid revisionId, Guid lineId, byte[] rowVersion, CancellationToken ct = default);

    /// <summary>Section 14 — an authorized project-specific override with reason/user/timestamp/old/new value audit.</summary>
    Task<ProjectRevisionDto> OverrideLineFieldAsync(Guid revisionId, Guid lineId, ProjectLineOverrideRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<ProjectLineOverrideHistoryDto>> GetOverrideHistoryAsync(Guid lineId, CancellationToken ct = default);
}
