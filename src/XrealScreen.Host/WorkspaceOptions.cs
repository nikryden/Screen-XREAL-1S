using XrealScreen.Core.Tracking;
using XrealScreen.Core.Workspace;

namespace XrealScreen.Host;

public enum TrackingSource
{
    /// <summary>XREAL One-series glasses over the USB network link.</summary>
    Glasses,

    /// <summary>Synthetic head sweep (no glasses needed).</summary>
    Simulated,

    /// <summary>No tracking: screens stay in front of the eyes (diagnostic).</summary>
    Fixed,
}

/// <summary>Everything a workspace session needs. Defaults = what was verified in the glasses on 2026-09-26.</summary>
public sealed record WorkspaceOptions
{
    public int ScreenCount { get; init; } = 3;

    public UltrawideMode Mode { get; init; } = UltrawideMode.Off;

    public LayoutPreset Preset { get; init; } = LayoutPreset.Arc;

    public float DistanceMeters { get; init; } = 1.5f;

    public TrackingSource Source { get; init; } = TrackingSource.Glasses;

    /// <summary>off | balanced | strong | ultra (see <see cref="StabilizerSettings"/>).</summary>
    public string Stabilizer { get; init; } = "strong";

    public AutoCenterSettings AutoCenter { get; init; } = new();

    public int GlassesRefreshHz { get; init; } = 120;

    public double PredictMs { get; init; } = 12;

    public float FilterBeta { get; init; } = 0.02f;

    public float AccelGate { get; init; } = 0.6f;

    /// <summary>Neck model: leaning/nodding moves the eyes, so screens come closer when you lean in.</summary>
    public bool NeckModel { get; init; } = true;

    /// <summary>Run other monitors at the glasses refresh during the session (ADR-0009).</summary>
    public bool SyncDesktopRefresh { get; init; } = true;
}
