using System.Windows;

namespace FamilyGuard.UI.Views;

public partial class StatusWindow : Window
{
    public StatusWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
