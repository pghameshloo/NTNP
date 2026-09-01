using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTNP.Pricing.Contracts.Audit;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>Screen 22 (Section 22/30) — Audit Logs (Admin only, enforced server-side).</summary>
public sealed partial class AuditLogViewModel : ViewModelBase
{
    private readonly AuditLogApiClient _api;

    [ObservableProperty] private string? _entityType;
    [ObservableProperty] private string? _entityId;
    [ObservableProperty] private DateTimeOffset? _fromUtc;
    [ObservableProperty] private DateTimeOffset? _toUtc;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private AuditLogEntryDto? _selectedEntry;

    public ObservableCollection<AuditLogEntryDto> Entries { get; } = new();

    public AuditLogViewModel(AuditLogApiClient api) => _api = api;

    public override Task OnNavigatedToAsync() => SearchAsync();

    [RelayCommand]
    private async Task SearchAsync() => await RunBusyAsync(async () =>
    {
        var result = await _api.SearchAsync(new AuditLogQuery(
            string.IsNullOrWhiteSpace(EntityType) ? null : EntityType, string.IsNullOrWhiteSpace(EntityId) ? null : EntityId,
            null, null, FromUtc, ToUtc, Page, 50));
        Entries.Clear();
        foreach (var e in result.Items) Entries.Add(e);
        TotalPages = result.TotalPages;
    });
}
