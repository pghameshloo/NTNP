using System.Windows.Controls;
using System.Windows.Input;
using NTNP.Pricing.Desktop.ViewModels;

namespace NTNP.Pricing.Desktop.Views;

public partial class BodyEsTemplatesView : UserControl
{
    public BodyEsTemplatesView() => InitializeComponent();

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is BodyEsTemplatesViewModel vm)
            await vm.SearchCommand.ExecuteAsync(null);
    }
}
