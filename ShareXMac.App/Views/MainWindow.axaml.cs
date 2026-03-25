using System;
using Avalonia.Controls;
using ShareXMac.App.ViewModels;

namespace ShareXMac.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainWindowViewModel vm)
        {
            _ = vm.History.LoadHistoryAsync();
        }
    }
}
