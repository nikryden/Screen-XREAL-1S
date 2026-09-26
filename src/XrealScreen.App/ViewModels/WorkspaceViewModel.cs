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

    partial void OnScreenCountChanged(double value) => Recompute();

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
