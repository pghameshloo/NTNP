using NTNP.Pricing.Contracts.Projects;

namespace NTNP.Pricing.Application.Projects;

public interface IProjectRevisionService
{
    Task<ProjectRevisionDto> GetAsync(Guid revisionId, CancellationToken ct = default);
    Task<IReadOnlyList<RevisionListItemDto>> ListAsync(Guid projectId, CancellationToken ct = default);

    /// <summary>Section 13 — "Create New Revision Using Latest Prices": a new revision, old one preserved untouched.</summary>
    Task<ProjectRevisionDto> CreateNewRevisionUsingLatestPricesAsync(Guid projectId, CancellationToken ct = default);

    Task<RevisionComparisonDto> CompareAsync(Guid fromRevisionId, Guid toRevisionId, CancellationToken ct = default);

    Task<ProjectRevisionDto> SubmitForApprovalAsync(Guid revisionId, SubmitForApprovalRequest request, CancellationToken ct = default);
    Task<ProjectRevisionDto> DecideApprovalAsync(Guid revisionId, ApprovalDecisionRequest request, CancellationToken ct = default);
    Task<ProjectRevisionDto> LockAsync(Guid revisionId, LockRevisionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ApprovalHistoryItemDto>> GetApprovalHistoryAsync(Guid revisionId, CancellationToken ct = default);
}
