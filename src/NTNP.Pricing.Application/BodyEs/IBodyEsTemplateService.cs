using NTNP.Pricing.Contracts.BodyEs;
using NTNP.Pricing.Contracts.Common;

namespace NTNP.Pricing.Application.BodyEs;

public interface IBodyEsTemplateService
{
    Task<PagedResult<BodyEsTemplateDto>> SearchAsync(PagedQuery query, Guid? productFamilyId, Guid? panelTypeId, CancellationToken ct = default);
    Task<BodyEsTemplateDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<BodyEsTemplateDto> CreateAsync(CreateBodyEsTemplateRequest request, CancellationToken ct = default);
    Task<BodyEsTemplateDto> UpdateAsync(Guid id, UpdateBodyEsTemplateRequest request, CancellationToken ct = default);
    Task<BodyEsTemplateDto> ApproveAsync(Guid id, byte[] rowVersion, CancellationToken ct = default);
}
