using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NTNP.Pricing.Contracts.Projects;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>
/// Screen 11 (Section 22/13) — Project List. Also serves as the "بررسی و تأیید" (Approval queue,
/// status filter = PendingApproval) nav destination and the entry point into a project's full
/// workspace (Section 22 screens 13-20), which needs a specific project chosen first.
/// </summary>
public sealed partial class ProjectListViewModel : ViewModelBase
{
    private readonly ProjectsApiClient _api;
    private readonly INavigationService _navigation;

    public static readonly IReadOnlyList<(string Value, string Label)> StatusOptions = new[]
    {
        ("", "همه وضعیت‌ها"), ("Draft", "پیش‌نویس"), ("UnderEngineeringReview", "بررسی فنی"),
        ("UnderCommercialReview", "بررسی بازرگانی"), ("PendingApproval", "در انتظار تأیید"),
        ("Approved", "تأیید شده"), ("Rejected", "رد شده"), ("Locked", "قفل شده"), ("Superseded", "جایگزین شده"),
    };

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _statusFilter;
    [ObservableProperty] private int _page = 1;
    [ObservableProperty] private int _totalPages = 1;

    public ObservableCollection<ProjectListItemDto> Projects { get; } = new();

    public ProjectListViewModel(ProjectsApiClient api, INavigationService navigation)
    {
        _api = api;
        _navigation = navigation;
    }

    public override Task OnNavigatedToAsync() => SearchAsync();

    [RelayCommand]
    private async Task SearchAsync() => await RunBusyAsync(async () =>
    {
        var result = await _api.SearchAsync(SearchText, Page, 25, StatusFilter);
        Projects.Clear();
        foreach (var p in result.Items) Projects.Add(p);
        TotalPages = result.TotalPages;
    });

    [RelayCommand]
    private async Task OpenAsync(ProjectListItemDto? project)
    {
        if (project is null) return;
        var workspace = App.Services.GetRequiredService<ProjectWorkspaceViewModel>();
        workspace.ProjectId = project.Id;
        await _navigation.NavigateToAsync(workspace, $"{project.ProjectCode} — {project.ProjectName}", "پروژه‌ها");
    }
}
