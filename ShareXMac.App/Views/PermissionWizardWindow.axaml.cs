using Avalonia.Controls;
using ShareXMac.App.ViewModels;
using ShareXMac.Core.Permissions;

namespace ShareXMac.App.Views;

public partial class PermissionWizardWindow : Window
{
    public PermissionWizardWindow(IPermissionManager permissionManager)
    {
        InitializeComponent();
        DataContext = new PermissionWizardViewModel(permissionManager, Close);
    }
}
