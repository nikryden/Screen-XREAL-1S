using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using XrealScreen.Core.Abstractions;
using XrealScreen.Core.Tracking;
using XrealScreen.Device.Simulated;
using XrealScreen.Device.XrealOne;

namespace XrealScreen.App.ViewModels;

/// <summary>
/// Runs the IMU → HeadTracker pipeline on a background task and publishes the pose to the UI at ~30 Hz.
/// </summary>
public sealed partial class TrackingViewModel : ObservableObject, IDisposable
{
    private readonly Lock _poseLock = new();
    private readonly DispatcherQueueTimer _uiTimer;
    private HeadTracker _tracker = new();
    private HeadPose _latestPose;
    private float _latestReferenceYaw;
    private long _samples;
    private long _samplesAtLastTick;
    private long _lastTickTimestamp;
    private string? _error;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public TrackingViewModel(DispatcherQueue dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _uiTimer = dispatcher.CreateTimer();
        _uiTimer.Interval = TimeSpan.FromMilliseconds(33);
        _uiTimer.Tick += (_, _) => PublishPose();
        ApplyAutoCenterSettings();
    }

    public event EventHandler? PoseUpdated;

    public IReadOnlyList<string> SourceNames { get; } = ["Simulated head (no glasses)", "XREAL One-series glasses (USB network)", "No tracking — screens follow the head (diagnostic)"];

    public IReadOnlyList<string> StabilizerNames { get; } = ["Off", "Balanced", "Strong (recommended)", "Ultra steady"];

    /// <summary>0 off, 1 balanced, 2 strong, 3 ultra (see StabilizerSettings).</summary>
    [ObservableProperty]
    public partial int StabilizerIndex { get; set; } = 2;

    public string StabilizerPreset => StabilizerIndex switch { 0 => "off", 1 => "balanced", 3 => "ultra", _ => "strong" };

    /// <summary>Screens come closer when you lean or nod in (the 1S measures rotation only).</summary>
    [ObservableProperty]
    public partial bool NeckModel { get; set; } = true;

    /// <summary>True while a pose is shown: the preview runs, or a workspace session feeds poses.</summary>
    public bool HasPose => IsRunning || _externalPose;

    private bool _externalPose;

    /// <summary>Shows a pose coming from the running workspace session (UI thread).</summary>
    public void ApplyExternalPose(XrealScreen.Core.Tracking.HeadPose pose)
    {
        _externalPose = true;
        Yaw = pose.YawDegrees;
        Pitch = pose.PitchDegrees;
        Roll = pose.RollDegrees;
        PoseUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void ClearExternalPose()
    {
        _externalPose = false;
        PoseUpdated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Set by the app while a workspace session runs; blocks the standalone preview.</summary>
    public bool SessionActive
    {
        get => _sessionActive;
        set
        {
            _sessionActive = value;
            StartCommand.NotifyCanExecuteChanged();
        }
    }

    private bool _sessionActive;

    public IReadOnlyList<string> PolicyNames { get; } = ["Manual — stays until you recenter", "Follow — moves when you turn beyond the dead-zone"];

    [ObservableProperty]
    public partial int SourceIndex { get; set; }

    [ObservableProperty]
    public partial int PolicyIndex { get; set; }

    [ObservableProperty]
    public partial double DeadZoneDegrees { get; set; } = 20;

    [ObservableProperty]
    public partial double SmoothingSeconds { get; set; } = 0.6;

    [ObservableProperty]
    public partial double MaxFollowSpeed { get; set; } = 90;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand), nameof(RecenterCommand))]
    public partial bool IsRunning { get; set; }

    [ObservableProperty]
    public partial double Yaw { get; set; }

    [ObservableProperty]
    public partial double Pitch { get; set; }

    [ObservableProperty]
    public partial double Roll { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = "Stopped";

    public bool IsFollow => PolicyIndex == 1;

    partial void OnPolicyIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsFollow));
        ApplyAutoCenterSettings();
    }

    partial void OnDeadZoneDegreesChanged(double value) => ApplyAutoCenterSettings();

    partial void OnSmoothingSecondsChanged(double value) => ApplyAutoCenterSettings();

    partial void OnMaxFollowSpeedChanged(double value) => ApplyAutoCenterSettings();

    [RelayCommand(CanExecute = nameof(CanStart))]
    private void Start()
    {
        IImuSource source = SourceIndex == 1
            ? new XrealOneImuSource()
            : new SyntheticImuSource(new SyntheticImuOptions { Motion = SyntheticMotion.YawSweep, DurationSeconds = 3600, RealTime = true, AmplitudeDegrees = 60, PeriodSeconds = 8 });

        _tracker = new HeadTracker();
        ApplyAutoCenterSettings();
        _error = null;
        _samples = 0;
        _samplesAtLastTick = 0;
        _lastTickTimestamp = Stopwatch.GetTimestamp();
        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunAsync(source, _cts.Token));
        IsRunning = true;
        Status = $"Connecting to {source.Name}…";
        _uiTimer.Start();
    }

    private bool CanStart() => !IsRunning && !SessionActive;

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private async Task StopAsync()
    {
        _cts?.Cancel();
        if (_loop is not null)
        {
            try
            {
                await _loop.ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
            }
        }

        _uiTimer.Stop();
        IsRunning = false;
        Status = "Stopped";
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Recenter() => _tracker.RequestRecenter();

    private async Task RunAsync(IImuSource source, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var sample in source.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var pose = _tracker.Update(sample);
                lock (_poseLock)
                {
                    _latestPose = pose;
                    _latestReferenceYaw = _tracker.AutoCenter.ReferenceYaw;
                    _samples++;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException ex)
        {
            lock (_poseLock)
            {
                _error = ex.Message;
            }
        }
    }

    private void PublishPose()
    {
        HeadPose pose;
        long samples;
        string? error;
        lock (_poseLock)
        {
            pose = _latestPose;
            samples = _samples;
            error = _error;
        }

        Yaw = pose.YawDegrees;
        Pitch = pose.PitchDegrees;
        Roll = pose.RollDegrees;

        double elapsed = Stopwatch.GetElapsedTime(_lastTickTimestamp).TotalSeconds;
        if (elapsed >= 1)
        {
            double rate = (samples - _samplesAtLastTick) / elapsed;
            _samplesAtLastTick = samples;
            _lastTickTimestamp = Stopwatch.GetTimestamp();
            Status = error is not null ? $"Error: {error}" : $"Running · {rate:F0} samples/s";
        }

        if (error is not null && _loop is { IsCompleted: true })
        {
            _uiTimer.Stop();
            IsRunning = false;
            Status = $"Error: {error}";
        }

        PoseUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyAutoCenterSettings()
    {
        _tracker.AutoCenter.Settings = new AutoCenterSettings
        {
            Policy = PolicyIndex == 1 ? RecenterPolicy.Follow : RecenterPolicy.Manual,
            DeadZoneDegrees = (float)DeadZoneDegrees,
            SmoothingSeconds = (float)SmoothingSeconds,
            MaxFollowSpeedDegrees = (float)MaxFollowSpeed,
        };
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _uiTimer.Stop();
    }
}
