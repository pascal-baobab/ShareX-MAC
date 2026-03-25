using Avalonia.Controls;

namespace ShareXMac.App.Views;

/// <summary>
/// Single history entry card showing a thumbnail, filename, date, and capture mode badge.
/// Right-click context menu provides Copy to Clipboard, Show in Finder, and Delete actions.
/// </summary>
public partial class HistoryItemCard : UserControl
{
    public HistoryItemCard()
    {
        InitializeComponent();
    }
}
