using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using XrealScreen.App.ViewModels;

namespace XrealScreen.App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    /// <summary>App-wide view models (single window app; a DI container arrives with XrealScreen.Host in M5).</summary>
    public static WorkspaceViewModel Workspace { get; private set; } = null!;

    public static TrackingViewModel Tracking { get; private set; } = null!;

    public static DeviceViewModel Device { get; private set; } = null!;

    public static MainWindow MainWindow { get; private set; } = null!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var dispatcher = DispatcherQueue.GetForCurrentThread();
        Workspace = new WorkspaceViewModel();
        Tracking = new TrackingViewModel(dispatcher);
        Device = new DeviceViewModel();

        MainWindow = new MainWindow();
        MainWindow.Closed += (_, _) => Tracking.Dispose();
        _window = MainWindow;
        _window.Activate();
    }
}
