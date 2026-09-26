using Microsoft.UI.Xaml.Controls;
using XrealScreen.App.ViewModels;

namespace XrealScreen.App.Pages;

public sealed partial class ScreensPage : Page
{
    public ScreensPage()
    {
        InitializeComponent();
    }

    public WorkspaceViewModel Workspace => App.Workspace;
}
