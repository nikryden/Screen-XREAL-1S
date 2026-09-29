using CommunityToolkit.Mvvm.ComponentModel;
using XrealScreen.Core.Workspace;

namespace XrealScreen.App.ViewModels;

/// <summary>Screens page: how many virtual screens, ultrawide mode and layout.</summary>
public sealed partial class WorkspaceViewModel : ObservableObject
{
    public WorkspaceViewModel() => Recompute();

    public event EventHandler? LayoutChanged;

    public IReadOnlyList<string> UltrawideModeNames { get; } = UltrawideModes.All.Select(UltrawideModes.GetDisplayName).ToList();

    public IReadOnlyList<string> PresetNames { get; } = ["Arc", "Grid"];

    /// <summary>0 = glasses anchor, 1 = app head tracking.</summary>
    public IReadOnlyList<string> KindNames { get; } =
    [
        "Glasses anchor — most stable. Glasses menu: Anchor mode + UltraWide 32:9, 16:18 or 21:9. 1–3 screens.",
        "App head tracking — curved screens, up to 6. Glasses menu: Follow mode, UltraWide Off, Stabilizer off.",
    ];

    [ObservableProperty]
    public partial int KindIndex { get; set; }

    public bool IsAppTracking => KindIndex == 1;

    partial void OnKindIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsAppTracking));
        OnPropertyChanged(nameof(IsGlassesAnchor));
        RefreshGlassesSignal();
    }

    public bool IsGlassesAnchor => KindIndex == 0;

    public IReadOnlyList<string> AnchorAspectNames { get; } = ["16:9 (recommended)", "16:10", "Fill the height"];

    /// <summary>Index = (int)AnchorAspect.</summary>
    [ObservableProperty]
    public partial int AnchorAspectIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnchorGapText))]
    public partial double AnchorGapPixels { get; set; } = 10;

    public string AnchorGapText => $"{Math.Round(AnchorGapPixels):F0} px";

    public XrealScreen.Core.Workspace.AnchorAspect AnchorAspect => (XrealScreen.Core.Workspace.AnchorAspect)Math.Clamp(AnchorAspectIndex, 0, 2);

    /// <summary>True when count/gap/shape changed since the last Apply or Start.</summary>
    [ObservableProperty]
    public partial bool HasPendingScreenChanges { get; set; }

    /// <summary>Glasses image size used for the preview (the real signal, or a 32:9 example).</summary>
    public XrealScreen.Core.Workspace.Resolution AnchorCanvas { get; private set; } = new(3840, 1080);

    public bool AnchorCanvasIsExample { get; private set; } = true;

    public IReadOnlyList<XrealScreen.Core.Workspace.PixelRect> AnchorRects { get; private set; } = [];

    /// <summary>Raised when the glasses-anchor preview must be redrawn.</summary>
    public event EventHandler? AnchorPreviewChanged;

    partial void OnAnchorAspectIndexChanged(int value) => OnAnchorSettingChanged();

    partial void OnAnchorGapPixelsChanged(double value) => OnAnchorSettingChanged();

    private void OnAnchorSettingChanged()
    {
        HasPendingScreenChanges = true;
        RefreshGlassesSignal();
    }

    public void MarkScreenSettingsApplied() => HasPendingScreenChanges = false;

    /// <summary>What the glasses send right now (glasses-anchor mode), e.g. "32:9 (3840×1080) → 2 screens".</summary>
    [ObservableProperty]
    public partial string GlassesSignal { get; set; } = string.Empty;

    public void RefreshGlassesSignal()
    {
        int count = double.IsNaN(ScreenCount) ? 1 : (int)Math.Clamp(Math.Round(ScreenCount), 1, XrealScreen.Core.Workspace.AnchorSplit.MaxScreens);
        int gap = (int)Math.Round(AnchorGapPixels);
        GlassesSignal = XrealScreen.Host.GlassesSignal.Describe(count, gap, AnchorAspect);

        var current = XrealScreen.Host.GlassesSignal.CurrentResolution();
        AnchorCanvasIsExample = current is not { } r || !XrealScreen.Core.Workspace.AnchorSplit.IsUltrawideSignal(r);
        AnchorCanvas = AnchorCanvasIsExample ? new XrealScreen.Core.Workspace.Resolution(3840, 1080) : current!.Value;
        AnchorRects = XrealScreen.Core.Workspace.AnchorSplit.Split(AnchorCanvas, count, gap, AnchorAspect);
        AnchorPreviewChanged?.Invoke(this, EventArgs.Empty);
    }

    [ObservableProperty]
    public partial double ScreenCount { get; set; } = 3;

    /// <summary>Index into <see cref="UltrawideModes.All"/> (same order as the enum).</summary>
    [ObservableProperty]
    public partial int UltrawideModeIndex { get; set; }

    [ObservableProperty]
    public partial int PresetIndex { get; set; }

    [ObservableProperty]
    public partial double DistanceMeters { get; set; } = 1.5;

    [ObservableProperty]
    public partial string Summary { get; set; } = string.Empty;

    public IReadOnlyList<ScreenPlacement> Placements { get; private set; } = [];

    public UltrawideMode UltrawideMode => UltrawideModes.All[Math.Clamp(UltrawideModeIndex, 0, UltrawideModes.All.Count - 1)];

    partial void OnScreenCountChanged(double value)
    {
        Recompute();
        if (IsGlassesAnchor)
        {
            OnAnchorSettingChanged();
        }
    }

    partial void OnUltrawideModeIndexChanged(int value) => Recompute();

    partial void OnPresetIndexChanged(int value) => Recompute();

    partial void OnDistanceMetersChanged(double value) => Recompute();

    private void Recompute()
    {
        int count = double.IsNaN(ScreenCount) ? 1 : (int)Math.Clamp(Math.Round(ScreenCount), 1, WorkspaceLayout.MaxScreens);
        var screens = Enumerable.Range(1, count).Select(i => new ScreenSpec(i, UltrawideMode)).ToList();
        var settings = new WorkspaceSettings
        {
            Preset = PresetIndex == 1 ? LayoutPreset.Grid : LayoutPreset.Arc,
            DistanceMeters = (float)Math.Clamp(DistanceMeters, 0.5, 5),
        };
        Placements = WorkspaceLayout.Compute(screens, settings);

        var res = UltrawideModes.GetResolution(UltrawideMode);
        float left = Placements.Max(p => p.YawDegrees + p.WidthMeters / p.DistanceMeters * 90f / MathF.PI);
        Summary = $"{count} × {res} ({UltrawideModes.GetDisplayName(UltrawideMode)}) · spans ±{left:F0}° · virtual monitors created in M2";
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }
}
