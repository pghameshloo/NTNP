using System.Windows.Controls;
using System.Windows.Input;
using NTNP.Pricing.Desktop.ViewModels;

namespace NTNP.Pricing.Desktop.Views;

public partial class CustomersView : UserControl
{
    public CustomersView() => InitializeComponent();

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is CustomersViewModel vm)
            await vm.SearchCommand.ExecuteAsync(null);
    }
}
