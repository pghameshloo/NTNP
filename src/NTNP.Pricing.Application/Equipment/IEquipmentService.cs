using NTNP.Pricing.Contracts.Common;
using NTNP.Pricing.Contracts.Equipment;

namespace NTNP.Pricing.Application.Equipment;

public interface IEquipmentService
{
    Task<PagedResult<EquipmentDto>> SearchAsync(
        PagedQuery query, bool includeInactive, string? category, bool missingPriceOnly, CancellationToken ct = default);
    Task<EquipmentDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<EquipmentDto> CreateAsync(CreateEquipmentRequest request, CancellationToken ct = default);
    Task<EquipmentDto> UpdateAsync(Guid id, UpdateEquipmentRequest request, CancellationToken ct = default);
    Task BulkSetActiveAsync(IReadOnlyList<Guid> ids, bool isActive, CancellationToken ct = default);
    Task<IReadOnlyList<EquipmentPriceDto>> GetPriceHistoryAsync(Guid equipmentId, CancellationToken ct = default);
    Task<EquipmentPriceDto> AddPriceAsync(CreateEquipmentPriceRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<EquipmentDto>> GetMissingOrExpiredPriceReportAsync(int staleDays, CancellationToken ct = default);
}
