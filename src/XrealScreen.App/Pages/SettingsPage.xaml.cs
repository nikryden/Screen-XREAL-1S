using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel;

namespace XrealScreen.App.Pages;

public sealed partial class SettingsPage : Page
{
    private bool _loading = true;

    public SettingsPage()
    {
        InitializeComponent();
        ThemeRadio.SelectedIndex = App.MainWindow.CurrentTheme switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0,
        };
        TrayToggle.IsOn = App.Preferences.KeepRunningInTray;
        Loaded += async (_, _) =>
        {
            await LoadStartupStateAsync().ConfigureAwait(true);
            _loading = false;
        };
    }

    private async Task LoadStartupStateAsync()
    {
        var task = await StartupTask.GetAsync(App.StartupTaskId);
        StartWithWindowsToggle.IsOn = task.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
        ShowStartupHint(task.State);
    }

    private async void StartWithWindows_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        try
        {
            var task = await StartupTask.GetAsync(App.StartupTaskId);
            var state = StartWithWindowsToggle.IsOn ? await task.RequestEnableAsync() : Disable(task);
            _loading = true;
            StartWithWindowsToggle.IsOn = state is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
            _loading = false;
            ShowStartupHint(state);
        }
        catch (Exception ex)
        {
            CrashLog.Write("StartupTask", ex);
            StartupHint.Text = $"Could not change the startup setting: {ex.Message}";
            StartupHint.Visibility = Visibility.Visible;
        }
    }

    private static StartupTaskState Disable(StartupTask task)
    {
        task.Disable();
        return task.State;
    }

    private void ShowStartupHint(StartupTaskState state)
    {
        string? hint = state switch
        {
            StartupTaskState.DisabledByUser => "Windows blocks this because start-up was turned off for XrealScreen in Task Manager → Startup apps. Turn it on there.",
            StartupTaskState.DisabledByPolicy => "Start-up is disabled by a system policy.",
            _ => null,
        };
        StartupHint.Text = hint ?? string.Empty;
        StartupHint.Visibility = hint is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Tray_Toggled(object sender, RoutedEventArgs e)
    {
        App.Preferences.KeepRunningInTray = TrayToggle.IsOn;
        App.Preferences.Save();
    }

    private void ThemeRadio_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        App.MainWindow.SetTheme(ThemeRadio.SelectedIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default,
        });
}
