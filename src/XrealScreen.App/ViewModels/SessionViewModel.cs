using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using XrealScreen.Core.Tracking;
using XrealScreen.Core.Workspace;
using XrealScreen.Host;

namespace XrealScreen.App.ViewModels;

/// <summary>
/// Owns the workspace session (WorkspaceEngine): builds options from the Screens and Tracking pages,
/// persists them, and reports state/log/pose to the UI thread.
/// </summary>
public sealed partial class SessionViewModel : ObservableObject, IDisposable
{
    private readonly DispatcherQueue _dispatcher;
    private readonly WorkspaceEngine _engine = new();
    private readonly WorkspaceSettingsStore _store = new();
    private readonly WorkspaceViewModel _workspace;
    private readonly TrackingViewModel _tracking;
    private readonly DispatcherQueueTimer _poseTimer;
    private bool _applyingEngineDistance;

    public SessionViewModel(DispatcherQueue dispatcher, WorkspaceViewModel workspace, TrackingViewModel tracking)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _workspace = workspace;
        _tracking = tracking;
        _poseTimer = dispatcher.CreateTimer();
        _poseTimer.Interval = TimeSpan.FromMilliseconds(33);
        _poseTimer.Tick += (_, _) => _tracking.ApplyExternalPose(_engine.CurrentPose);
        _engine.StateChanged += (_, state) => _dispatcher.TryEnqueue(() => OnEngineState(state));
        _engine.Log += (_, message) => _dispatcher.TryEnqueue(() => AddLog(message));

        // Distance: hotkeys (Ctrl+Alt+Up/Down) update the slider; the slider moves the screens live.
        _engine.DistanceChanged += (_, meters) => _dispatcher.TryEnqueue(() =>
        {
            _applyingEngineDistance = true;
            _workspace.DistanceMeters = Math.Round(meters, 2);
            _applyingEngineDistance = false;
        });
        _workspace.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspaceViewModel.DistanceMeters) && IsRunning && !_applyingEngineDistance)
            {
                _engine.SetDistance((float)_workspace.DistanceMeters);
            }
        };
        ApplySettings(_store.Load());
    }

    public ObservableCollection<string> Log { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunning), nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand), nameof(RecenterCommand))]
    public partial WorkspaceState State { get; set; }

    [ObservableProperty]
    public partial string StatusTitle { get; set; } = "Workspace stopped";

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Glasses: UltraWide Off, Follow mode, Stabilizer off. Then select Start workspace.";

    [ObservableProperty]
    public partial InfoBarSeverity Severity { get; set; } = InfoBarSeverity.Informational;

    public bool IsRunning => State == WorkspaceState.Running;

    public bool IsIdle => State is WorkspaceState.Stopped or WorkspaceState.Faulted;

    [RelayCommand(CanExecute = nameof(IsIdle))]
    private async Task StartAsync()
    {
        if (_tracking.IsRunning)
        {
            await _tracking.StopCommand.ExecuteAsync(null).ConfigureAwait(true);
        }

        var options = BuildOptions();
        _store.Save(options);
        StatusTitle = "Starting…";
        StatusMessage = "Creating virtual monitors and preparing the glasses.";
        Severity = InfoBarSeverity.Informational;
        try
        {
            await Task.Run(() => _engine.StartAsync(options, CancellationToken.None)).ConfigureAwait(true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException or IOException or TimeoutException)
        {
            StatusTitle = "Could not start";
            StatusMessage = ex.Message;
            Severity = InfoBarSeverity.Error;
        }
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private Task StopAsync() => Task.Run(_engine.StopAsync);

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Recenter() => _engine.Recenter();

    /// <summary>Stops the session from the window-closed handler (engine continuations do not need the UI thread).</summary>
    public void StopBlocking() => _engine.StopAsync().GetAwaiter().GetResult();

    public void Dispose()
    {
        StopBlocking();
        _engine.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private void OnEngineState(WorkspaceState state)
    {
        State = state;
        _tracking.SessionActive = state is WorkspaceState.Starting or WorkspaceState.Running or WorkspaceState.Stopping;
        if (state == WorkspaceState.Running)
        {
            _poseTimer.Start();
            StatusTitle = "Workspace running";
            StatusMessage = "Move the mouse right from your desk monitor onto the virtual screens. Ctrl+Alt+R recenter · Ctrl+Alt+Plus/Minus closer/farther · Ctrl+Alt+Q stop.";
            Severity = InfoBarSeverity.Success;
        }
        else if (state is WorkspaceState.Stopped or WorkspaceState.Faulted)
        {
            _poseTimer.Stop();
            _tracking.ClearExternalPose();
            if (state == WorkspaceState.Faulted)
            {
                StatusTitle = "Workspace stopped with an error";
                StatusMessage = _engine.LastError?.Message ?? "See the log.";
                Severity = InfoBarSeverity.Error;
            }
            else if (Severity != InfoBarSeverity.Error)
            {
                StatusTitle = "Workspace stopped";
                StatusMessage = "Your monitor layout and refresh rates were restored.";
                Severity = InfoBarSeverity.Informational;
            }
        }
    }

    private void AddLog(string message)
    {
        Log.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        while (Log.Count > 200)
        {
            Log.RemoveAt(Log.Count - 1);
        }
    }

    private WorkspaceOptions BuildOptions() => new()
    {
        ScreenCount = (int)Math.Clamp(Math.Round(double.IsNaN(_workspace.ScreenCount) ? 1 : _workspace.ScreenCount), 1, WorkspaceLayout.MaxScreens),
        Mode = _workspace.UltrawideMode,
        Preset = _workspace.PresetIndex == 1 ? LayoutPreset.Grid : LayoutPreset.Arc,
        DistanceMeters = (float)_workspace.DistanceMeters,
        Source = _tracking.SourceIndex switch { 0 => TrackingSource.Simulated, 2 => TrackingSource.Fixed, _ => TrackingSource.Glasses },
        Stabilizer = _tracking.StabilizerPreset,
        NeckModel = _tracking.NeckModel,
        Axes = (TrackingAxes)Math.Clamp(_tracking.AxesIndex, 0, 2),
        AutoCenter = new AutoCenterSettings
        {
            Policy = _tracking.PolicyIndex == 1 ? RecenterPolicy.Follow : RecenterPolicy.Manual,
            DeadZoneDegrees = (float)_tracking.DeadZoneDegrees,
            SmoothingSeconds = (float)_tracking.SmoothingSeconds,
            MaxFollowSpeedDegrees = (float)_tracking.MaxFollowSpeed,
        },
    };

    private void ApplySettings(WorkspaceOptions o)
    {
        _workspace.ScreenCount = o.ScreenCount;
        _workspace.UltrawideModeIndex = Math.Max(0, UltrawideModes.All.ToList().IndexOf(o.Mode));
        _workspace.PresetIndex = o.Preset == LayoutPreset.Grid ? 1 : 0;
        _workspace.DistanceMeters = o.DistanceMeters;
        _tracking.SourceIndex = o.Source switch { TrackingSource.Simulated => 0, TrackingSource.Fixed => 2, _ => 1 };
        _tracking.StabilizerIndex = o.Stabilizer switch { "off" => 0, "balanced" => 1, "ultra" => 3, _ => 2 };
        _tracking.NeckModel = o.NeckModel;
        _tracking.AxesIndex = (int)o.Axes;
        _tracking.PolicyIndex = o.AutoCenter.Policy == RecenterPolicy.Follow ? 1 : 0;
        _tracking.DeadZoneDegrees = o.AutoCenter.DeadZoneDegrees;
        _tracking.SmoothingSeconds = o.AutoCenter.SmoothingSeconds;
        _tracking.MaxFollowSpeed = o.AutoCenter.MaxFollowSpeedDegrees;
    }
}
