using System.Windows;
using System.Windows.Controls;

namespace FamilyGuard.UI.Views;

public partial class EventLogWindow : Window
{
    public EventLogWindow()
    {
        InitializeComponent();
        LoadEvents();
    }

    private void LoadEvents()
    {
        // TODO: Wire to IEventStore.QueryRecent via DI
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        LoadEvents();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadEvents();
    }
}
