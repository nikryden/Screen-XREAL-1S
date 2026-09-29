using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using XrealScreen.App.ViewModels;
using XrealScreen.Core.Workspace;

namespace XrealScreen.App.Controls;

/// <summary>
/// Flat "unrolled cylinder" view of the workspace: screens by yaw/pitch, plus the
/// glasses' field of view at the current head pose.
/// </summary>
public sealed partial class WorkspacePreview : UserControl
{
    private const double YawRange = 200;   // degrees shown horizontally
    private const double PitchRange = 110; // degrees shown vertically
    private const double FovH = 46;        // XREAL 1S ~52° diagonal @ 16:10 [from vendor spec]
    private const double FovV = 29;

    private readonly Canvas _canvas = new();
    private WorkspaceViewModel? _workspace;
    private TrackingViewModel? _tracking;

    public WorkspacePreview()
    {
        Content = new Border
        {
            Child = _canvas,
            CornerRadius = new CornerRadius(8),
            Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
        };
        SizeChanged += (_, _) => Redraw();
        Loaded += (_, _) => Attach();
        Unloaded += (_, _) => Detach();
        MinHeight = 200;
    }

    private void Attach()
    {
        _workspace = App.Workspace;
        _tracking = App.Tracking;
        _workspace.LayoutChanged += OnChanged;
        _tracking.PoseUpdated += OnChanged;
        Redraw();
    }

    private void Detach()
    {
        if (_workspace is not null)
        {
            _workspace.LayoutChanged -= OnChanged;
        }

        if (_tracking is not null)
        {
            _tracking.PoseUpdated -= OnChanged;
        }
    }

    private void OnChanged(object? sender, EventArgs e) => Redraw();

    private void Redraw()
    {
        _canvas.Children.Clear();
        if (_workspace is null || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        double sx = ActualWidth / YawRange;
        double sy = ActualHeight / PitchRange;
        double cx = ActualWidth / 2;
        double cy = ActualHeight / 2;

        var screenFill = (Brush)Application.Current.Resources["AccentFillColorSecondaryBrush"];
        var screenStroke = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        var textBrush = (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"];

        foreach (ScreenPlacement p in _workspace.Placements)
        {
            double wDeg = p.WidthMeters / p.DistanceMeters * 180 / Math.PI;
            double hDeg = Math.Atan2(p.HeightMeters, p.DistanceMeters) * 180 / Math.PI;
            var rect = new Rectangle
            {
                Width = wDeg * sx,
                Height = hDeg * sy,
                Fill = screenFill,
                Stroke = screenStroke,
                StrokeThickness = 1,
                RadiusX = 4,
                RadiusY = 4,
            };
            // Positive yaw = left.
            Canvas.SetLeft(rect, cx - p.YawDegrees * sx - rect.Width / 2);
            Canvas.SetTop(rect, cy - p.PitchDegrees * sy - rect.Height / 2);
            _canvas.Children.Add(rect);

            string number = p.ScreenId.ToString(System.Globalization.CultureInfo.CurrentCulture);
            var label = new TextBlock { Text = p.ScreenId == App.Workspace.PrimaryScreenIndex ? number + " · primary" : number, Foreground = textBrush, FontSize = 12 };
            Canvas.SetLeft(label, Canvas.GetLeft(rect) + 6);
            Canvas.SetTop(label, Canvas.GetTop(rect) + 4);
            _canvas.Children.Add(label);
        }

        if (_tracking is { HasPose: true })
        {
            var fov = new Rectangle
            {
                Width = FovH * sx,
                Height = FovV * sy,
                Stroke = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"],
                StrokeThickness = 2,
                RadiusX = 6,
                RadiusY = 6,
            };
            Canvas.SetLeft(fov, cx - _tracking.Yaw * sx - fov.Width / 2);
            Canvas.SetTop(fov, cy - _tracking.Pitch * sy - fov.Height / 2);
            _canvas.Children.Add(fov);
        }
    }
}
