using Microsoft.UI.Xaml.Controls;
using XrealScreen.App.ViewModels;

namespace XrealScreen.App.Pages;

public sealed partial class ScreensPage : Page
{
    public ScreensPage()
    {
        InitializeComponent();
        Workspace.RefreshGlassesSignal();
    }

    public WorkspaceViewModel Workspace => App.Workspace;

    private void RefreshSignal_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => Workspace.RefreshGlassesSignal();
}
