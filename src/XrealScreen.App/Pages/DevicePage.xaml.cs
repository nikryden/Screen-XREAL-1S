using Microsoft.UI.Xaml.Controls;
using XrealScreen.App.ViewModels;

namespace XrealScreen.App.Pages;

public sealed partial class DevicePage : Page
{
    public DevicePage()
    {
        InitializeComponent();
    }

    public DeviceViewModel Device => App.Device;
}
