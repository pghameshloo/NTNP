using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Projects;

namespace NTNP.Pricing.Desktop.Services.Api;

public sealed class ProjectsApiClient : ApiClientBase
{
    public ProjectsApiClient(HttpClient http, IServerConnectionSettingsService serverSettings, AppSession session) : base(http, serverSettings, session)
    {
    }

    public Task<PagedResult<ProjectListItemDto>> SearchAsync(string? search, int page, int pageSize, string? status, CancellationToken ct = default) =>
        GetAsync<PagedResult<ProjectListItemDto>>(
            $"api/projects?search={Uri.EscapeDataString(search ?? "")}&page={page}&pageSize={pageSize}&status={Uri.EscapeDataString(status ?? "")}", ct);

    public Task<ProjectDto> GetAsync(Guid id, CancellationToken ct = default) => GetAsync<ProjectDto>($"api/projects/{id}", ct);
    public Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken ct = default) => PostAsync<ProjectDto>("api/projects", request, ct);
    public Task<ProjectDto> UpdateInfoAsync(Guid id, UpdateProjectInfoRequest request, CancellationToken ct = default) => PutAsync<ProjectDto>($"api/projects/{id}/info", request, ct);

    public Task<ProjectRevisionDto> UpdatePricingSettingsAsync(Guid id, UpdateProjectPricingSettingsRequest request, CancellationToken ct = default) =>
        PutAsync<ProjectRevisionDto>($"api/projects/{id}/pricing-settings", request, ct);
}
