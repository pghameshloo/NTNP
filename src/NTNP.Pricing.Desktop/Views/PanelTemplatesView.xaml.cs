using System.Windows.Controls;
using System.Windows.Input;
using NTNP.Pricing.Desktop.ViewModels;

namespace NTNP.Pricing.Desktop.Views;

public partial class PanelTemplatesView : UserControl
{
    public PanelTemplatesView() => InitializeComponent();

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is PanelTemplatesViewModel vm)
            await vm.SearchCommand.ExecuteAsync(null);
    }
}
