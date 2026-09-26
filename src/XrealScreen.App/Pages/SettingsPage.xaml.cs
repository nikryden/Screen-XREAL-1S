using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace XrealScreen.App.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        ThemeRadio.SelectedIndex = App.MainWindow.CurrentTheme switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0,
        };
    }

    private void ThemeRadio_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        App.MainWindow.SetTheme(ThemeRadio.SelectedIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default,
        });
}
