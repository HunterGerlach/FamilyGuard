using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FamilyGuard.UI.ViewModels;

namespace FamilyGuard.UI.Resources;

/// <summary>
/// Generates tray icons as on-disk PNG files referenced via file:// URI.
/// H.NotifyIcon accepts PNG via BitmapImage(Uri) — confirmed working
/// in production (ui.log: "Tray icon created successfully" at 13:34).
/// </summary>
public static class TrayIconGenerator
{
    private static readonly string IconDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FamilyGuard", "icons");

    private static readonly Dictionary<TrayIconState, BitmapImage> Cache = new();

    public static BitmapImage GetImageSource(TrayIconState state)
    {
        if (Cache.TryGetValue(state, out var cached))
            return cached;

        Directory.CreateDirectory(IconDir);

        var color = state switch
        {
            TrayIconState.Normal => Color.FromRgb(76, 175, 80),
            TrayIconState.Warning => Color.FromRgb(255, 193, 7),
            TrayIconState.ActionTaken => Color.FromRgb(244, 67, 54),
            TrayIconState.Disconnected => Color.FromRgb(158, 158, 158),
            _ => Colors.Gray
        };

        var filePath = Path.Combine(IconDir, $"tray-{state}.png");
        RenderShieldToPng(color, 64, filePath);

        var image = new BitmapImage(new Uri(filePath, UriKind.Absolute));
        image.Freeze();
        Cache[state] = image;
        return image;
    }

    private static void RenderShieldToPng(Color fillColor, int size, string outputPath)
    {
        var visual = new DrawingVisual();
        using (var ctx = visual.RenderOpen())
        {
            var brush = new SolidColorBrush(fillColor);
            brush.Freeze();
            var borderBrush = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));
            borderBrush.Freeze();
            var pen = new Pen(borderBrush, size * 0.06);
            pen.Freeze();

            double s = size;
            double m = s * 0.06;

            // Shield shape
            var geometry = new StreamGeometry();
            using (var sgc = geometry.Open())
            {
                sgc.BeginFigure(new Point(s / 2, m), true, true);
                sgc.ArcTo(new Point(s - m, s * 0.22), new Size(s * 0.4, s * 0.2), 0, false, SweepDirection.Clockwise, true, false);
                sgc.LineTo(new Point(s - m, s * 0.5), true, false);
                sgc.QuadraticBezierTo(new Point(s - m, s * 0.72), new Point(s / 2, s - m), true, false);
                sgc.QuadraticBezierTo(new Point(m, s * 0.72), new Point(m, s * 0.5), true, false);
                sgc.LineTo(new Point(m, s * 0.22), true, false);
                sgc.ArcTo(new Point(s / 2, m), new Size(s * 0.4, s * 0.2), 0, false, SweepDirection.Clockwise, true, false);
            }
            geometry.Freeze();
            ctx.DrawGeometry(brush, pen, geometry);

            // "D" letter
            var letterBrush = new SolidColorBrush(Colors.White);
            letterBrush.Freeze();
            var text = new FormattedText(
                "D",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                s * 0.5,
                letterBrush,
                96);
            ctx.DrawText(text, new Point((s - text.Width) / 2, (s - text.Height) / 2));
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(outputPath);
        encoder.Save(stream);
    }
}
