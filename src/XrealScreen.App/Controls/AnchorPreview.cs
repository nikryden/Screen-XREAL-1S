using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using XrealScreen.App.ViewModels;

namespace XrealScreen.App.Controls;

/// <summary>
/// Picture of the glasses-anchor workspace: the glasses image (dark frame, to scale) with the
/// virtual screens drawn where they will appear, labelled with number and resolution.
/// </summary>
public sealed partial class AnchorPreview : UserControl
{
    private readonly Canvas _canvas = new();
    private WorkspaceViewModel? _workspace;

    public AnchorPreview()
    {
        Content = _canvas;
        SizeChanged += (_, _) => Redraw();
        Loaded += (_, _) =>
        {
            _workspace = App.Workspace;
            _workspace.AnchorPreviewChanged += OnChanged;
            _workspace.RefreshGlassesSignal();
        };
        Unloaded += (_, _) =>
        {
            if (_workspace is not null)
            {
                _workspace.AnchorPreviewChanged -= OnChanged;
            }
        };
        MinHeight = 120;
    }

    private void OnChanged(object? sender, EventArgs e) => Redraw();

    private void Redraw()
    {
        _canvas.Children.Clear();
        if (_workspace is null || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var glasses = _workspace.AnchorCanvas;
        double scale = Math.Min(ActualWidth / glasses.Width, (ActualHeight - 24) / glasses.Height);
        double frameW = glasses.Width * scale, frameH = glasses.Height * scale;
        double ox = (ActualWidth - frameW) / 2, oy = 0;

        var res = Application.Current.Resources;
        var frame = new Rectangle
        {
            Width = frameW,
            Height = frameH,
            RadiusX = 10,
            RadiusY = 10,
            Fill = new SolidColorBrush(Microsoft.UI.Colors.Black),
            Stroke = (Brush)res["ControlStrokeColorDefaultBrush"],
            StrokeThickness = 1,
        };
        Canvas.SetLeft(frame, ox);
        Canvas.SetTop(frame, oy);
        _canvas.Children.Add(frame);

        int n = 1;
        foreach (var r in _workspace.AnchorRects)
        {
            var screen = new Rectangle
            {
                Width = r.Width * scale,
                Height = r.Height * scale,
                RadiusX = 3,
                RadiusY = 3,
                Fill = (Brush)res["AccentFillColorSecondaryBrush"],
                Stroke = (Brush)res["AccentFillColorDefaultBrush"],
                StrokeThickness = 1,
            };
            Canvas.SetLeft(screen, ox + r.X * scale);
            Canvas.SetTop(screen, oy + r.Y * scale);
            _canvas.Children.Add(screen);

            var label = new TextBlock
            {
                Text = $"{n++}\n{r.Width}×{r.Height}",
                Foreground = (Brush)res["TextOnAccentFillColorPrimaryBrush"],
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
            };
            label.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(label, ox + (r.X + r.Width / 2.0) * scale - label.DesiredSize.Width / 2);
            Canvas.SetTop(label, oy + (r.Y + r.Height / 2.0) * scale - label.DesiredSize.Height / 2);
            _canvas.Children.Add(label);
        }

        var caption = new TextBlock
        {
            Text = _workspace.AnchorCanvasIsExample
                ? $"Example: glasses in UltraWide 32:9 ({glasses})"
                : $"Glasses image {glasses} — held still by the glasses (Anchor mode)",
            Foreground = (Brush)res["TextFillColorSecondaryBrush"],
            FontSize = 12,
        };
        Canvas.SetLeft(caption, ox);
        Canvas.SetTop(caption, oy + frameH + 4);
        _canvas.Children.Add(caption);
    }
}
