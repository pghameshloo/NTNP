using Microsoft.EntityFrameworkCore;
using NTNP.Pricing.Application.Common;
using NTNP.Pricing.Application.Exceptions;
using NTNP.Pricing.Contracts.Mto;
using NTNP.Pricing.Domain.Calculation;
using NTNP.Pricing.Domain.Entities;

namespace NTNP.Pricing.Application.Projects;

public sealed class MtoService : IMtoService
{
    private readonly IApplicationDbContext _db;

    public MtoService(IApplicationDbContext db) => _db = db;

    public async Task<MtoResultDto> GetMtoAsync(Guid revisionId, CancellationToken ct = default)
    {
        var revision = await _db.ProjectRevisions.AsNoTracking()
            .Include(r => r.Lines).ThenInclude(l => l.BomItems)
            .Include(r => r.Lines).ThenInclude(l => l.BodyEsItems)
            .FirstOrDefaultAsync(r => r.Id == revisionId, ct)
            ?? throw new NotFoundException(nameof(ProjectRevision), revisionId);

        var electrical = MtoCalculator.CalculateElectricalMto(revision);
        var bodyEs = MtoCalculator.CalculateBodyEsMto(revision);
        var combined = MtoCalculator.CalculateCombinedMto(revision);

        return new MtoResultDto(ToDtos(electrical), ToDtos(bodyEs), ToDtos(combined));
    }

    private static IReadOnlyList<MtoLineDto> ToDtos(IReadOnlyList<MtoLine> lines) =>
        lines.Select((m, idx) => new MtoLineDto(
            idx + 1, m.Code, m.PartNumber, m.Description, m.Brand, m.Model, m.Unit, m.TotalRequiredQuantity,
            m.PurchaseCurrencyCode, m.SnapshotUnitCostIrr, m.TotalProcurementCostIrr, m.RelatedPanelTypes, null, m.Kind))
        .ToList();
}
