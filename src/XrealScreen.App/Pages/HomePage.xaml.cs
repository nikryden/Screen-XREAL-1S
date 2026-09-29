using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
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

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker(App.MainWindow.AppWindow.Id);
            picker.FileTypeFilter.Add(".json");
            var result = await picker.PickSingleFileAsync();
            if (result is not null)
            {
                await Session.ImportSettingsAsync(result.Path).ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            CrashLog.Write("Import", ex);
        }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileSavePicker(App.MainWindow.AppWindow.Id)
            {
                SuggestedFileName = $"XrealScreen settings {DateTime.Now:yyyy-MM-dd}",
            };
            picker.FileTypeChoices.Add("XrealScreen settings", [".json"]);
            var result = await picker.PickSaveFileAsync();
            if (result is not null)
            {
                Session.ExportSettings(result.Path);
            }
        }
        catch (Exception ex)
        {
            CrashLog.Write("Export", ex);
        }
    }
}
