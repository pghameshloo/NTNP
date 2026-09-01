using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using NTNP.Pricing.Contracts.Dashboard;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>Screen 2 (Section 22/24) — Dashboard KPIs and charts.</summary>
public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly DashboardApiClient _api;

    [ObservableProperty] private DashboardSummaryDto? _summary;

    public ObservableCollection<StatusBarItem> ProjectsByStatusBars { get; } = new();
    public ObservableCollection<StatusBarItem> CostCompositionBars { get; } = new();

    public DashboardViewModel(DashboardApiClient api) => _api = api;

    public override Task OnNavigatedToAsync() => LoadAsync();

    private async Task LoadAsync() => await RunBusyAsync(async () =>
    {
        Summary = await _api.GetSummaryAsync();

        ProjectsByStatusBars.Clear();
        var maxCount = Math.Max(1, Summary.ProjectsByStatus.Count == 0 ? 1 : Summary.ProjectsByStatus.Max(s => s.Count));
        foreach (var s in Summary.ProjectsByStatus)
            ProjectsByStatusBars.Add(new StatusBarItem(s.Status, s.Count.ToString(CultureInfo.InvariantCulture), (double)s.Count / maxCount));

        CostCompositionBars.Clear();
        var maxAmount = Math.Max(1m, Summary.CostComposition.Count == 0 ? 1m : Summary.CostComposition.Max(c => c.AmountIrr));
        foreach (var c in Summary.CostComposition)
            CostCompositionBars.Add(new StatusBarItem(c.Category, c.AmountIrr.ToString("N0", CultureInfo.InvariantCulture), (double)(c.AmountIrr / maxAmount)));
    });
}

/// <summary>One row of a hand-drawn horizontal bar chart (Section 23: no external charting dependency needed for a handful of KPI bars).</summary>
public sealed record StatusBarItem(string Label, string ValueText, double FractionOfMax);
