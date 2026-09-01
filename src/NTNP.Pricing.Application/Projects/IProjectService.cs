using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Projects;

namespace NTNP.Pricing.Application.Projects;

public interface IProjectService
{
    Task<PagedResult<ProjectListItemDto>> SearchAsync(PagedQuery query, string? status, CancellationToken ct = default);
    Task<ProjectDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken ct = default);
    Task<ProjectDto> UpdateInfoAsync(Guid id, UpdateProjectInfoRequest request, CancellationToken ct = default);

    /// <summary>Section 5 (wizard step "Pricing Settings") — updates project defaults and recalculates the current (mutable) revision.</summary>
    Task<ProjectRevisionDto> UpdatePricingSettingsAsync(Guid id, UpdateProjectPricingSettingsRequest request, CancellationToken ct = default);
}
