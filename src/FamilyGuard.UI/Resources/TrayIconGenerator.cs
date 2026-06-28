using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FamilyGuard.UI.ViewModels;

namespace FamilyGuard.UI.Resources;

/// <summary>
/// Generates tray icons as PNG-backed ImageSource for H.NotifyIcon.
/// Uses a shield shape with state-specific colors.
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

        var image = CreateShieldIcon(color, 256);
        image.Freeze();
        Cache[state] = image;
        return image;
    }

    /// <summary>
    /// Creates a shield-shaped icon with the given color.
    /// The shield represents protection — fitting for DAD.
    /// </summary>
    private static BitmapSource CreateShieldIcon(Color fillColor, int size)
    {
        var visual = new DrawingVisual();
        using (var ctx = visual.RenderOpen())
        {
            var brush = new SolidColorBrush(fillColor);
            brush.Freeze();
            var borderBrush = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));
            borderBrush.Freeze();
            var pen = new Pen(borderBrush, size * 0.04);
            pen.Freeze();

            // Shield path: rounded top, pointed bottom
            var s = (double)size;
            var m = s * 0.1; // margin
            var geometry = new StreamGeometry();
            using (var sgc = geometry.Open())
            {
                sgc.BeginFigure(new Point(s / 2, m), true, true);
                // Top-right curve
                sgc.ArcTo(new Point(s - m, s * 0.2), new Size(s * 0.4, s * 0.2), 0, false, SweepDirection.Clockwise, true, false);
                // Right side
                sgc.LineTo(new Point(s - m, s * 0.5), true, false);
                // Bottom-right curve to point
                sgc.QuadraticBezierTo(new Point(s - m, s * 0.7), new Point(s / 2, s - m), true, false);
                // Bottom-left curve from point
                sgc.QuadraticBezierTo(new Point(m, s * 0.7), new Point(m, s * 0.5), true, false);
                // Left side
                sgc.LineTo(new Point(m, s * 0.2), true, false);
                // Top-left curve
                sgc.ArcTo(new Point(s / 2, m), new Size(s * 0.4, s * 0.2), 0, false, SweepDirection.Clockwise, true, false);
            }
            geometry.Freeze();

            ctx.DrawGeometry(brush, pen, geometry);

            // Draw "D" letter in the center
            var letterBrush = new SolidColorBrush(Colors.White);
            letterBrush.Freeze();
            var formattedText = new FormattedText(
                "D",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                s * 0.45,
                letterBrush,
                96);
            var textOrigin = new Point(
                (s - formattedText.Width) / 2,
                (s - formattedText.Height) / 2 - s * 0.02);
            ctx.DrawText(formattedText, textOrigin);
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        // Convert to PNG-backed BitmapImage for reliable icon conversion
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var stream = new MemoryStream();
        encoder.Save(stream);
        stream.Position = 0;

        var pngImage = new BitmapImage();
        pngImage.BeginInit();
        pngImage.StreamSource = stream;
        pngImage.CacheOption = BitmapCacheOption.OnLoad;
        pngImage.EndInit();
        pngImage.Freeze();

        return pngImage;
    }
}
