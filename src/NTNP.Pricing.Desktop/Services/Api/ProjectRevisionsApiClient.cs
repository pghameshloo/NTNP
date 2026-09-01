using NTNP.Pricing.Contracts.Mto;
using NTNP.Pricing.Contracts.Projects;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class ProjectRevisionsApiClient : ApiClientBase
{
    public ProjectRevisionsApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<IReadOnlyList<RevisionListItemDto>> ListForProjectAsync(Guid projectId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<RevisionListItemDto>>($"api/projects/{projectId}/revisions", ct);

    public Task<ProjectRevisionDto> GetAsync(Guid revisionId, CancellationToken ct = default) => GetAsync<ProjectRevisionDto>($"api/project-revisions/{revisionId}", ct);

    public Task<ProjectRevisionDto> CreateNewRevisionUsingLatestPricesAsync(Guid projectId, CancellationToken ct = default) =>
        PostAsync<ProjectRevisionDto>($"api/projects/{projectId}/revisions/create-using-latest-prices", null, ct);

    public Task<RevisionComparisonDto> CompareAsync(Guid fromRevisionId, Guid toRevisionId, CancellationToken ct = default) =>
        GetAsync<RevisionComparisonDto>($"api/project-revisions/compare?fromRevisionId={fromRevisionId}&toRevisionId={toRevisionId}", ct);

    // --- Lineup / BOM generator ---

    public Task<ProjectRevisionDto> AddLineAsync(Guid revisionId, AddProjectLineRequest request, CancellationToken ct = default) =>
        PostAsync<ProjectRevisionDto>($"api/project-revisions/{revisionId}/lines", request, ct);

    public Task<ProjectRevisionDto> UpdateLineQuantityAsync(Guid revisionId, Guid lineId, UpdateProjectLineQuantityRequest request, CancellationToken ct = default) =>
        PutAsync<ProjectRevisionDto>($"api/project-revisions/{revisionId}/lines/{lineId}/quantity", request, ct);

    public Task<ProjectRevisionDto> RemoveLineAsync(Guid revisionId, Guid lineId, byte[] rowVersion, CancellationToken ct = default) =>
        DeleteAsync<ProjectRevisionDto>($"api/project-revisions/{revisionId}/lines/{lineId}", rowVersion, ct);

    public Task<ProjectRevisionDto> OverrideLineFieldAsync(Guid revisionId, Guid lineId, ProjectLineOverrideRequest request, CancellationToken ct = default) =>
        PostAsync<ProjectRevisionDto>($"api/project-revisions/{revisionId}/lines/{lineId}/override", request, ct);

    public Task<IReadOnlyList<ProjectLineOverrideHistoryDto>> GetOverrideHistoryAsync(Guid lineId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ProjectLineOverrideHistoryDto>>($"api/project-lines/{lineId}/override-history", ct);

    // --- MTO ---

    public Task<MtoResultDto> GetMtoAsync(Guid revisionId, CancellationToken ct = default) => GetAsync<MtoResultDto>($"api/project-revisions/{revisionId}/mto", ct);

    // --- Approval workflow ---

    public Task<ProjectRevisionDto> SubmitForApprovalAsync(Guid revisionId, SubmitForApprovalRequest request, CancellationToken ct = default) =>
        PostAsync<ProjectRevisionDto>($"api/project-revisions/{revisionId}/submit", request, ct);

    public Task<ProjectRevisionDto> DecideApprovalAsync(Guid revisionId, ApprovalDecisionRequest request, CancellationToken ct = default) =>
        PostAsync<ProjectRevisionDto>($"api/project-revisions/{revisionId}/decide", request, ct);

    public Task<ProjectRevisionDto> LockAsync(Guid revisionId, LockRevisionRequest request, CancellationToken ct = default) =>
        PostAsync<ProjectRevisionDto>($"api/project-revisions/{revisionId}/lock", request, ct);

    public Task<IReadOnlyList<ApprovalHistoryItemDto>> GetApprovalHistoryAsync(Guid revisionId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ApprovalHistoryItemDto>>($"api/project-revisions/{revisionId}/approval-history", ct);
}
