using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FamilyGuard.UI.ViewModels;

namespace FamilyGuard.UI.Resources;

/// <summary>
/// Generates tray icons programmatically at runtime using WPF rendering.
/// Avoids shipping separate .ico files for each state.
/// </summary>
public static class TrayIconGenerator
{
    private static readonly Dictionary<TrayIconState, System.Drawing.Icon> Cache = new();

    public static System.Drawing.Icon GetIcon(TrayIconState state)
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

        var icon = CreateCircleIcon(color, 16);
        Cache[state] = icon;
        return icon;
    }

    private static System.Drawing.Icon CreateCircleIcon(Color color, int size)
    {
        var visual = new DrawingVisual();
        using (var ctx = visual.RenderOpen())
        {
            var brush = new SolidColorBrush(color);
            var pen = new Pen(new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), 1);
            ctx.DrawEllipse(brush, pen, new Point(size / 2.0, size / 2.0), size / 2.0 - 1, size / 2.0 - 1);
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        // Convert WPF bitmap to System.Drawing.Icon via PNG stream
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var pngStream = new System.IO.MemoryStream();
        encoder.Save(pngStream);
        pngStream.Position = 0;

        using var gdiBitmap = new System.Drawing.Bitmap(pngStream);
        var hIcon = gdiBitmap.GetHicon();
        return System.Drawing.Icon.FromHandle(hIcon);
    }
}
