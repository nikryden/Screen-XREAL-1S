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
        UnhandledException += (_, e) =>
        {
            CrashLog.Write("UI", e.Exception);
            e.Handled = true; // keep running; the session engine restores the desktop on its own
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) => CrashLog.Write("AppDomain", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            CrashLog.Write("Task", e.Exception);
            e.SetObserved();
        };
    }

    /// <summary>App-wide view models (single window app; a DI container arrives with XrealScreen.Host in M5).</summary>
    public static WorkspaceViewModel Workspace { get; private set; } = null!;

    public static TrackingViewModel Tracking { get; private set; } = null!;

    public static DeviceViewModel Device { get; private set; } = null!;

    public static SessionViewModel Session { get; private set; } = null!;

    public static MainWindow MainWindow { get; private set; } = null!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var dispatcher = DispatcherQueue.GetForCurrentThread();
        Workspace = new WorkspaceViewModel();
        Tracking = new TrackingViewModel(dispatcher);
        Device = new DeviceViewModel();
        Session = new SessionViewModel(dispatcher, Workspace, Tracking);

        MainWindow = new MainWindow();
        MainWindow.Closed += (_, _) =>
        {
            // Always hand the desktop back (virtual monitors, layout, refresh rates).
            Session.Dispose();
            Tracking.Dispose();
        };
        _window = MainWindow;
        _window.Activate();
    }
}
