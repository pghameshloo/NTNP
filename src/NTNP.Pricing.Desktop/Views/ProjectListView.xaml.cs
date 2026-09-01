using System.Windows.Controls;
using System.Windows.Input;
using NTNP.Pricing.Contracts.Projects;
using NTNP.Pricing.Desktop.ViewModels;

namespace NTNP.Pricing.Desktop.Views;

public partial class ProjectListView : UserControl
{
    public ProjectListView() => InitializeComponent();

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ProjectListViewModel vm)
            await vm.SearchCommand.ExecuteAsync(null);
    }

    private async void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid { SelectedItem: ProjectListItemDto project } && DataContext is ProjectListViewModel vm)
            await vm.OpenCommand.ExecuteAsync(project);
    }
}
