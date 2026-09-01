using System.Windows.Controls;
using System.Windows.Input;
using NTNP.Pricing.Contracts.Projects;
using NTNP.Pricing.Desktop.ViewModels;

namespace NTNP.Pricing.Desktop.Views;

public partial class ProjectWorkspaceView : UserControl
{
    public ProjectWorkspaceView() => InitializeComponent();

    private async void RevisionHistory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid { SelectedItem: RevisionListItemDto item } && DataContext is ProjectWorkspaceViewModel vm)
            await vm.OpenRevisionCommand.ExecuteAsync(item);
    }
}
