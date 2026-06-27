using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FamilyGuard.UI.ViewModels;

namespace FamilyGuard.UI.Resources;

/// <summary>
/// Generates tray icon images as WPF ImageSource at runtime.
/// Pure WPF — no System.Drawing or WinForms dependency.
/// </summary>
public static class TrayIconGenerator
{
    private static readonly Dictionary<TrayIconState, ImageSource> Cache = new();

    public static ImageSource GetImageSource(TrayIconState state)
    {
        if (Cache.TryGetValue(state, out var cached))
            return cached;

        var color = state switch
        {
            TrayIconState.Normal => Color.FromRgb(76, 175, 80),         // Green
            TrayIconState.Warning => Color.FromRgb(255, 193, 7),        // Yellow/Amber
            TrayIconState.ActionTaken => Color.FromRgb(244, 67, 54),    // Red/Orange
            TrayIconState.Disconnected => Color.FromRgb(158, 158, 158), // Gray
            _ => Colors.Gray
        };

        var image = CreateCircleImage(color, 32);
        image.Freeze();
        Cache[state] = image;
        return image;
    }

    private static BitmapSource CreateCircleImage(Color color, int size)
    {
        var visual = new DrawingVisual();
        using (var ctx = visual.RenderOpen())
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            var borderBrush = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
            borderBrush.Freeze();
            var pen = new Pen(borderBrush, 1.5);
            pen.Freeze();

            var center = new Point(size / 2.0, size / 2.0);
            var radius = size / 2.0 - 2;
            ctx.DrawEllipse(brush, pen, center, radius, radius);
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}
