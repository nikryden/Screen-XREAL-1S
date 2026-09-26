using Microsoft.UI.Xaml.Controls;
using XrealScreen.App.ViewModels;

namespace XrealScreen.App.Pages;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
    }

    public WorkspaceViewModel Workspace => App.Workspace;

    public TrackingViewModel Tracking => App.Tracking;

    public DeviceViewModel Device => App.Device;

    public SessionViewModel Session => App.Session;
}
