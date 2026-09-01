using NTNP.Pricing.Application.Projects;
using NTNP.Pricing.Application.Tests.TestDoubles;
using NTNP.Pricing.Contracts.Projects;
using NTNP.Pricing.Domain.Entities;
using NTNP.Pricing.Domain.Enums;
using NTNP.Pricing.Domain.Exceptions;
using NTNP.Pricing.Infrastructure.Persistence;

namespace NTNP.Pricing.Application.Tests;

/// <summary>
/// End-to-end coverage of the wizard flow (Section 21): create project → generate BOM from a panel
/// template (Section 15) → TOTAL reconciles (Section 19) → submit → approve → revision becomes
/// immutable (Section 13).
/// </summary>
public class ProjectWorkflowTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly FakeCurrentUserService _currentUser = new();
    private readonly FakeDateTimeProvider _clock = new();
    private readonly NoOpAuditService _audit = new();

    private readonly ProjectService _projectService;
    private readonly ProjectLineService _lineService;
    private readonly ProjectRevisionService _revisionService;

    private Customer _customer = null!;
    private PanelTemplate _template = null!;

    public ProjectWorkflowTests()
    {
        _db = TestDbContextFactory.Create();

        var snapshotBuilder = new BomSnapshotBuilder(_db);
        _projectService = new ProjectService(_db, _currentUser, _clock, _audit);
        _lineService = new ProjectLineService(_db, _currentUser, _clock, _audit, snapshotBuilder);
        _revisionService = new ProjectRevisionService(_db, _currentUser, _clock, _audit, snapshotBuilder);

        SeedMasterData();
    }

    public void Dispose() => _db.Dispose();

    private void SeedMasterData()
    {
        var eur = new Currency { Code = "EUR", Name = "Euro", Symbol = "€", CreatedByUserId = _currentUser.UserId, CreatedByUserName = "seed" };
        _db.Currencies.Add(eur);
        _db.SaveChanges();

        _db.ExchangeRates.Add(new ExchangeRate
        {
            CurrencyId = eur.Id, PurchaseRateToIrr = 1_800_000m, SellingRateToIrr = 1_800_000m,
            EffectiveAtUtc = _clock.UtcNow, IsActive = true, CreatedByUserId = _currentUser.UserId, CreatedByUserName = "seed",
        });

        var family = new ProductFamily { Code = "UNISAFE", Name = "UniSafe", CreatedByUserId = _currentUser.UserId, CreatedByUserName = "seed" };
        var panelType = new PanelType { Code = "INCOMING", Name = "INCOMING", CreatedByUserId = _currentUser.UserId, CreatedByUserName = "seed" };
        _db.ProductFamilies.Add(family);
        _db.PanelTypes.Add(panelType);

        var acb = new Domain.Entities.Equipment
        {
            Code = "ACB-001", DescriptionFa = "کلید", DescriptionEn = "Air Circuit Breaker", Unit = "EA",
            CreatedByUserId = _currentUser.UserId, CreatedByUserName = "seed",
        };
        _db.Equipment.Add(acb);
        _db.SaveChanges();

        _db.EquipmentPrices.Add(new EquipmentPrice
        {
            EquipmentId = acb.Id, PurchaseCurrencyCode = "EUR", ForeignUnitPrice = 800m,
            PurchaseExchangeRateSnapshot = 1_800_000m, FinalUnitCostIrr = 1_440_000_000m,
            EffectiveAtUtc = _clock.UtcNow, IsActive = true, CreatedByUserId = _currentUser.UserId, CreatedByUserName = "seed",
        });

        _customer = new Customer { CustomerCode = "C-1", CompanyName = "Test Co.", CreatedByUserId = _currentUser.UserId, CreatedByUserName = "seed" };
        _db.Customers.Add(_customer);
        _db.SaveChanges();

        _template = new PanelTemplate
        {
            TemplateCode = "PT-1", TemplateName = "Incoming Panel", ProductFamilyId = family.Id, PanelTypeId = panelType.Id,
            RevisionNumber = 1, Status = TemplateStatus.Approved, CreatedByUserId = _currentUser.UserId, CreatedByUserName = "seed",
        };
        _template.BomItems.Add(new PanelTemplateBomItem { EquipmentId = acb.Id, QuantityPerPanel = 2m, Unit = "EA", WastePercentage = 0m });
        _db.PanelTemplates.Add(_template);
        _db.SaveChanges();
    }

    [Fact]
    public async Task AddingALine_Generates_Bom_And_Reconciled_Totals()
    {
        var project = await _projectService.CreateAsync(new CreateProjectRequest(
            "PRJ-1", "Test Project", _customer.Id, null, null, null, null, null,
            "EUR", 0.15m, 0.85m, null, "Markup", 0.30m));

        var revision = await _lineService.AddLineAsync(project.CurrentRevisionId!.Value,
            new AddProjectLineRequest(_template.Id, "C01", 1m, 0m));

        var line = Assert.Single(revision.Lines);
        Assert.Equal(2_880_000_000m, line.EquipmentCostPerPanel); // 2 × 1,440,000,000
        Assert.True(revision.Totals.ReconciliationPassed);
        Assert.False(line.HasValidationErrors);
    }

    [Fact]
    public async Task ApprovedRevision_Becomes_Immutable()
    {
        var project = await _projectService.CreateAsync(new CreateProjectRequest(
            "PRJ-2", "Test Project 2", _customer.Id, null, null, null, null, null,
            "EUR", 0.15m, 0.85m, null, "Markup", 0.30m));

        var revision = await _lineService.AddLineAsync(project.CurrentRevisionId!.Value,
            new AddProjectLineRequest(_template.Id, "C01", 1m, 0m));

        await _revisionService.SubmitForApprovalAsync(revision.Id, new SubmitForApprovalRequest(revision.RowVersion));

        var submitted = await _revisionService.GetAsync(revision.Id);
        var approved = await _revisionService.DecideApprovalAsync(revision.Id, new ApprovalDecisionRequest(true, "Looks good", submitted.RowVersion));

        Assert.Equal("Approved", approved.Status);

        var reloaded = await _revisionService.GetAsync(revision.Id);
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _lineService.AddLineAsync(revision.Id, new AddProjectLineRequest(_template.Id, "C02", 1m, 0m)));
    }

    [Fact]
    public async Task Cannot_Approve_Revision_With_MissingPrice_Line()
    {
        // Add a second equipment item with no price at all.
        var relay = new Domain.Entities.Equipment { Code = "RLY-999", DescriptionFa = "رله", DescriptionEn = "Relay", Unit = "EA",
            CreatedByUserId = _currentUser.UserId, CreatedByUserName = "seed" };
        _db.Equipment.Add(relay);
        await _db.SaveChangesAsync();
        var relayBomItem = new PanelTemplateBomItem { EquipmentId = relay.Id, QuantityPerPanel = 1m, Unit = "EA" };
        _template.BomItems.Add(relayBomItem);
        _db.PanelTemplateBomItems.Add(relayBomItem); // _template is already tracked — see ProjectLineService's comment on this pattern
        await _db.SaveChangesAsync();

        var project = await _projectService.CreateAsync(new CreateProjectRequest(
            "PRJ-3", "Test Project 3", _customer.Id, null, null, null, null, null,
            "EUR", 0.15m, 0.85m, null, "Markup", 0.30m));

        var revision = await _lineService.AddLineAsync(project.CurrentRevisionId!.Value,
            new AddProjectLineRequest(_template.Id, "C01", 1m, 0m));

        Assert.True(revision.Lines.Single().HasValidationErrors);
        Assert.NotEmpty(revision.Totals.ApprovalBlockers);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _revisionService.SubmitForApprovalAsync(revision.Id, new SubmitForApprovalRequest(revision.RowVersion)));
    }
}
