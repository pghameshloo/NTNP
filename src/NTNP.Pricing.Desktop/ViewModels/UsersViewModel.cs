using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NTNP.Pricing.Contracts.Auth;
using NTNP.Pricing.Desktop.Services;
using NTNP.Pricing.Desktop.Services.Api;
using Roles = NTNP.Pricing.Desktop.Services.DesktopRoles;

namespace NTNP.Pricing.Desktop.ViewModels;

/// <summary>Screen 21 (Section 22/6) — Users and Roles administration (Admin only, enforced server-side too).</summary>
public sealed partial class UsersViewModel : ViewModelBase
{
    private readonly UsersApiClient _api;
    private readonly IDialogService _dialogs;

    public static readonly IReadOnlyList<string> AllRoles = Roles.All;

    [ObservableProperty] private UserDto? _selectedUser;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isNew;

    [ObservableProperty] private string _formUserName = string.Empty;
    [ObservableProperty] private string _formEmail = string.Empty;
    [ObservableProperty] private string _formDisplayName = string.Empty;
    [ObservableProperty] private string _formPassword = string.Empty;
    [ObservableProperty] private bool _formIsActive = true;
    [ObservableProperty] private bool _formIsAdmin;
    [ObservableProperty] private bool _formIsEngineering;
    [ObservableProperty] private bool _formIsCommercial;
    [ObservableProperty] private bool _formIsApprover;
    [ObservableProperty] private bool _formIsViewer;

    public ObservableCollection<UserDto> Users { get; } = new();

    public UsersViewModel(UsersApiClient api, IDialogService dialogs)
    {
        _api = api;
        _dialogs = dialogs;
    }

    public override Task OnNavigatedToAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync() => await RunBusyAsync(async () =>
    {
        var list = await _api.ListAsync();
        Users.Clear();
        foreach (var u in list) Users.Add(u);
    });

    partial void OnSelectedUserChanged(UserDto? value)
    {
        if (value is null) return;
        IsNew = false;
        IsEditing = true;
        FormUserName = value.UserName;
        FormEmail = value.Email;
        FormDisplayName = value.DisplayName;
        FormPassword = string.Empty;
        FormIsActive = value.IsActive;
        SetRoleCheckboxes(value.Roles);
    }

    private void SetRoleCheckboxes(IReadOnlyList<string> roles)
    {
        FormIsAdmin = roles.Contains(Roles.Admin);
        FormIsEngineering = roles.Contains(Roles.Engineering);
        FormIsCommercial = roles.Contains(Roles.Commercial);
        FormIsApprover = roles.Contains(Roles.Approver);
        FormIsViewer = roles.Contains(Roles.Viewer);
    }

    private List<string> CollectSelectedRoles()
    {
        var roles = new List<string>();
        if (FormIsAdmin) roles.Add(Roles.Admin);
        if (FormIsEngineering) roles.Add(Roles.Engineering);
        if (FormIsCommercial) roles.Add(Roles.Commercial);
        if (FormIsApprover) roles.Add(Roles.Approver);
        if (FormIsViewer) roles.Add(Roles.Viewer);
        return roles;
    }

    [RelayCommand]
    private void New()
    {
        SelectedUser = null;
        IsNew = true;
        IsEditing = true;
        FormUserName = FormEmail = FormDisplayName = FormPassword = string.Empty;
        FormIsActive = true;
        SetRoleCheckboxes(Array.Empty<string>());
    }

    [RelayCommand] private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task SaveAsync() => await RunBusyAsync(async () =>
    {
        var roles = CollectSelectedRoles();
        if (roles.Count == 0)
        {
            ErrorMessage = "حداقل یک نقش باید انتخاب شود.";
            return;
        }

        if (IsNew)
        {
            if (string.IsNullOrWhiteSpace(FormPassword))
            {
                ErrorMessage = "رمز عبور برای کاربر جدید الزامی است.";
                return;
            }
            var created = await _api.CreateAsync(new CreateUserRequest(FormUserName, FormEmail, FormDisplayName, FormPassword, roles));
            await LoadAsync();
            SelectedUser = Users.FirstOrDefault(u => u.Id == created.Id);
        }
        else if (SelectedUser is not null)
        {
            var updated = await _api.UpdateAsync(SelectedUser.Id, new UpdateUserRequest(FormDisplayName, FormIsActive, roles, Array.Empty<byte>()));
            var index = Users.IndexOf(SelectedUser);
            if (index >= 0) Users[index] = updated;
            SelectedUser = updated;
        }
        IsEditing = false;
    });

    [RelayCommand]
    private async Task ResetPasswordAsync() => await RunBusyAsync(async () =>
    {
        if (SelectedUser is null || string.IsNullOrWhiteSpace(FormPassword))
        {
            ErrorMessage = "برای بازنشانی، رمز عبور جدید را وارد کنید.";
            return;
        }
        await _api.ResetPasswordAsync(SelectedUser.Id, FormPassword);
        FormPassword = string.Empty;
        _dialogs.ShowInfo("بازنشانی رمز عبور", "رمز عبور کاربر با موفقیت بازنشانی شد.");
    });
}
