using System.CommandLine;
using System.Numerics;
using XrealScreen.Core.Abstractions;
using XrealScreen.Core.Tracking;
using XrealScreen.Core.Workspace;
using XrealScreen.Device.Simulated;
using XrealScreen.Device.XrealOne;
using XrealScreen.Display;
using XrealScreen.Display.VirtualDisplayRs;
using XrealScreen.Render;
using XrealScreen.Render.Capture;
using XrealScreen.Render.Presenter;
using XrealScreen.Render.Scene;

namespace XrealScreen.Cli;

/// <summary>M3 render spike: virtual monitors → capture → head-tracked panels on the glasses.</summary>
internal static class RenderCommands
{
    public static Command Create()
    {
        var screens = new Option<int>("--screens") { Description = "Number of virtual monitors (1-6).", DefaultValueFactory = _ => 1 };
        var mode = new Option<UltrawideMode>("--mode") { Description = "Virtual monitor size: Off (16:9), Wide21x9, Wide32x9, Tall16x18.", DefaultValueFactory = _ => UltrawideMode.Off };
        var source = new Option<string>("--source") { Description = "Head tracking: glasses | sim.", DefaultValueFactory = _ => "glasses" };
        var seconds = new Option<double>("--seconds") { Description = "Stop after this long (Esc on the glasses window stops earlier).", DefaultValueFactory = _ => 120 };
        var follow = new Option<bool>("--follow") { Description = "Auto center: follow the head beyond a 20° dead-zone." };
        var hz = new Option<int>("--hz") { Description = "Glasses refresh to request (UltraWide Off offers 60/90/120).", DefaultValueFactory = _ => 120 };
        var predict = new Option<double>("--predict-ms") { Description = "Pose prediction ahead of the late latch.", DefaultValueFactory = _ => 12 };
        var beta = new Option<float>("--beta") { Description = "Madgwick tilt-correction gain.", DefaultValueFactory = _ => 0.02f };
        var stabilize = new Option<string>("--stabilize") { Description = "Pose stabilizer: off | balanced | strong.", DefaultValueFactory = _ => "balanced" };
        var gate = new Option<float>("--accel-gate") { Description = "Skip tilt correction when | |a| - g | exceeds this (m/s²); 0 = off.", DefaultValueFactory = _ => 0.6f };
        var command = new Command("render", "Show virtual monitors fixed in space in the glasses (Ctrl+Alt+R = recenter, Ctrl+Alt+Q = stop).")
        {
            screens, mode, source, seconds, follow, hz, predict, beta, gate, stabilize,
        };
        command.SetAction(async (parse, ct) => await RunAsync(
            parse.GetValue(screens), parse.GetValue(mode), parse.GetValue(source)!, parse.GetValue(seconds),
            parse.GetValue(follow), parse.GetValue(hz), parse.GetValue(predict), parse.GetValue(beta), parse.GetValue(gate), parse.GetValue(stabilize)!, ct).ConfigureAwait(false));
        return command;
    }

    private static async Task<int> RunAsync(int screenCount, UltrawideMode mode, string source, double seconds, bool follow, int hz, double predictMs, float beta, float accelGate, string stabilize, CancellationToken ct)
    {
        GlassesPresenter.EnablePerMonitorDpi();
        var topology = new CcdDisplayTopology();
        var glasses = GlassesDisplayLocator.FindGlasses(topology.GetActiveMonitors());
        if (glasses is null)
        {
            Console.Error.WriteLine("Glasses monitor not found (extended display required).");
            return 2;
        }

        // 1. Glasses at native resolution and the requested refresh rate (session only).
        var native = topology.GetSupportedModes(glasses.GdiDeviceName).Where(m => m.BitsPerPixel == 32).OrderByDescending(m => m.Resolution.Width * m.Resolution.Height).ThenByDescending(m => m.RefreshHz).First();
        var wanted = topology.GetSupportedModes(glasses.GdiDeviceName).FirstOrDefault(m => m.Resolution == native.Resolution && m.RefreshHz == hz && m.BitsPerPixel == 32);
        if (wanted == default)
        {
            Console.WriteLine($"{hz} Hz not offered at {native.Resolution} (glasses UltraWide must be Off for 120 Hz); using {native}.");
            wanted = native;
        }

        var store = new LayoutSnapshotStore();
        if (!store.Exists)
        {
            await store.SaveAsync(topology.CaptureLayout(), ct).ConfigureAwait(false);
        }

        // 2. Virtual monitors + layout.
        using var provider = new VirtualDisplayRsProvider(topology);
        var specs = Enumerable.Range(1, Math.Clamp(screenCount, 1, WorkspaceLayout.MaxScreens)).Select(i => new ScreenSpec(i, mode)).ToList();
        var placements = WorkspaceLayout.Compute(specs, new WorkspaceSettings());
        var virtuals = new List<VirtualDisplay>();
        foreach (var spec in specs)
        {
            var vd = await provider.AddAsync(spec.Resolution, 60, ct).ConfigureAwait(false);
            virtuals.Add(vd);
            Console.WriteLine($"virtual monitor {spec.Id}: {vd.Resolution} → {vd.GdiDeviceName ?? "(not attached)"}");
        }

        await Task.Delay(1000, ct).ConfigureAwait(false);

        // Set the glasses mode AFTER adding virtual monitors: every topology change makes Windows
        // re-apply the glasses' saved mode ([verified-hw] 120 Hz fell back to 90 Hz).
        glasses = GlassesDisplayLocator.FindGlasses(topology.GetActiveMonitors()) ?? glasses;
        if (glasses.Resolution != wanted.Resolution || Math.Abs(glasses.RefreshHz - wanted.RefreshHz) > 1)
        {
            Console.WriteLine($"glasses → {wanted}: {(topology.TrySetMode(glasses.GdiDeviceName, wanted) ? "ok" : "rejected")}");
            await Task.Delay(1500, ct).ConfigureAwait(false);
        }

        var monitors = topology.GetActiveMonitors();
        glasses = GlassesDisplayLocator.FindGlasses(monitors) ?? glasses;
        double? measuredHz = OutputTiming.MeasureVblankHz(glasses.GdiDeviceName, TimeSpan.FromSeconds(0.5));
        Console.WriteLine($"glasses: {glasses.GdiDeviceName} {glasses.Resolution} @ {glasses.RefreshHz} Hz (vblank {measuredHz:F1} Hz) at ({glasses.X},{glasses.Y})");

        // 3. GPU, scene, capture.
        IntPtr glassesHmon = MonitorCapture.MonitorAt(glasses.X, glasses.Y);
        using var gd = GraphicsDevice.CreateForMonitor(glassesHmon);
        Console.WriteLine($"GPU: {gd.AdapterName}");
        using var scene = new WorkspaceScene(gd) { ClearColor = new Vortice.Mathematics.Color4(0.02f, 0.02f, 0.03f, 1f) };
        await MonitorCapture.RequestBorderlessAsync().ConfigureAwait(false);
        var captures = new List<MonitorCapture>();
        long capturedFrames = 0;
        for (int i = 0; i < virtuals.Count; i++)
        {
            var m = monitors.FirstOrDefault(x => x.GdiDeviceName == virtuals[i].GdiDeviceName);
            if (m is null)
            {
                Console.WriteLine($"virtual monitor {i + 1} not attached; skipped");
                continue;
            }

            var surface = scene.AddScreen(placements[i], m.Resolution.Width, m.Resolution.Height);
            var capture = new MonitorCapture(gd.Device, MonitorCapture.MonitorAt(m.X, m.Y));
            capture.FrameArrived += f =>
            {
                surface.Update(f.Texture, f.Width, f.Height);
                Interlocked.Increment(ref capturedFrames);
            };
            capture.Start();
            captures.Add(capture);
        }

        // 4. Head tracking on a background task; the render thread latches the newest pose.
        var tracker = new HeadTracker(new MadgwickFilter(beta) { AccelGate = accelGate });
        tracker.AutoCenter.Settings = new AutoCenterSettings { Policy = follow ? RecenterPolicy.Follow : RecenterPolicy.Manual };
        HeadPose latest = tracker.Current;
        var poseLock = new Lock();
        IImuSource imu = source == "sim"
            ? new SyntheticImuSource(new SyntheticImuOptions { Motion = SyntheticMotion.YawSweep, DurationSeconds = seconds + 10, RealTime = true, AmplitudeDegrees = 50, PeriodSeconds = 8 })
            : new XrealOneImuSource();
        using var stopTracking = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var trackingTask = Task.Run(async () =>
        {
            await foreach (var sample in imu.ReadAsync(stopTracking.Token).ConfigureAwait(false))
            {
                var pose = tracker.Update(sample);
                lock (poseLock)
                {
                    latest = pose;
                }
            }
        }, stopTracking.Token);

        float predictSeconds = (float)(predictMs / 1000.0);
        var stabilizer = new PoseStabilizer(StabilizerSettings.FromName(stabilize));
        long lastLatch = 0;

        // Called once per frame on the render thread: predict, then stabilize.
        Quaternion Latch()
        {
            Quaternion predicted;
            lock (poseLock)
            {
                predicted = latest.PredictRelative(predictSeconds);
            }

            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            float dt = lastLatch == 0 ? 0f : (float)System.Diagnostics.Stopwatch.GetElapsedTime(lastLatch, now).TotalSeconds;
            lastLatch = now;
            return stabilizer.Filter(predicted, dt);
        }

        // 5. Present on the glasses until Esc / timeout.
        using var presenter = new GlassesPresenter(gd, scene, GlassesOptics.Xreal1S, Latch, glasses.X, glasses.Y, glasses.Resolution.Width, glasses.Resolution.Height);
        presenter.RecenterRequested += tracker.RequestRecenter;
        presenter.RecenterRequested += stabilizer.Reset;
        presenter.Start();
        Console.WriteLine($"stabilizer: {stabilize}; prediction {predictMs} ms");
        Console.WriteLine($"rendering {captures.Count} screen(s) on the glasses for {seconds:F0} s — Ctrl+Alt+R recenters, Ctrl+Alt+Q stops");
        await Task.WhenAny(presenter.Completion, Task.Delay(TimeSpan.FromSeconds(seconds), ct)).ConfigureAwait(false);
        presenter.Stop();
        try
        {
            await presenter.Completion.ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is SharpGen.Runtime.SharpGenException or System.Runtime.InteropServices.COMException)
        {
            Console.Error.WriteLine($"render error: {ex.Message}");
        }

        // 6. Report and clean up.
        Console.WriteLine($"render: {presenter.GetStats()}");
        Console.WriteLine($"captured frames={Interlocked.Read(ref capturedFrames)}; imu samples={tracker.SampleCount}; final yaw={tracker.Current.YawDegrees:F1}° pitch={tracker.Current.PitchDegrees:F1}° bias={tracker.GyroBias}");
        await stopTracking.CancelAsync().ConfigureAwait(false);
        try
        {
            await trackingTask.ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException)
        {
        }

        foreach (var c in captures)
        {
            c.Dispose();
        }

        await provider.RemoveAllOwnedAsync(ct).ConfigureAwait(false);
        await Task.Delay(1000, ct).ConfigureAwait(false);
        var saved = await store.LoadAsync(ct).ConfigureAwait(false);
        if (saved is not null)
        {
            topology.ApplyLayout(saved);
            store.Delete();
            Console.WriteLine("virtual monitors removed, layout restored");
        }

        return 0;
    }
}
