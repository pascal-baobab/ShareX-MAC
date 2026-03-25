using Avalonia.Controls;

namespace ShareXMac.App.Views;

/// <summary>
/// History tab UserControl embedded in the MainWindow content area.
/// Provides search, filter chips, and a 3-column grid of HistoryItemCards.
/// </summary>
public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
    }
}
