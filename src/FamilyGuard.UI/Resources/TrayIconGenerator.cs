using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FamilyGuard.UI.ViewModels;

namespace FamilyGuard.UI.Resources;

/// <summary>
/// Generates tray icons as .ico files on disk.
/// H.NotifyIcon converts ImageSource to System.Drawing.Icon which
/// only accepts ICO format — PNG causes delayed APPCRASH.
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

        var filePath = Path.Combine(IconDir, $"tray-{state}.ico");
        WriteIcoFile(color, 32, filePath);

        var image = new BitmapImage(new Uri(filePath, UriKind.Absolute));
        image.Freeze();
        Cache[state] = image;
        return image;
    }

    private static void WriteIcoFile(Color fillColor, int size, string outputPath)
    {
        // Step 1: Render the shield to a WPF bitmap
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

        // Render with Pbgra32 (the only format RenderTargetBitmap supports)
        var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);

        // Step 2: Convert to Bgra32 pixel data for the ICO file
        var converted = new FormatConvertedBitmap(rtb, PixelFormats.Bgra32, null, 0);
        var stride = size * 4;
        var pixels = new byte[stride * size];
        converted.CopyPixels(pixels, stride, 0);

        // Step 3: Write ICO binary format
        using var fs = File.Create(outputPath);
        using var bw = new BinaryWriter(fs);

        // ICO header
        bw.Write((short)0);   // reserved
        bw.Write((short)1);   // type: icon
        bw.Write((short)1);   // image count

        // AND mask (1 bit per pixel, rows padded to 4 bytes)
        var andRowBytes = ((size + 31) / 32) * 4;
        var andMaskSize = andRowBytes * size;
        var bmpHeaderSize = 40;
        var imageDataSize = bmpHeaderSize + pixels.Length + andMaskSize;

        // Directory entry
        bw.Write((byte)(size < 256 ? size : 0));
        bw.Write((byte)(size < 256 ? size : 0));
        bw.Write((byte)0);    // no palette
        bw.Write((byte)0);    // reserved
        bw.Write((short)1);   // color planes
        bw.Write((short)32);  // bits per pixel
        bw.Write(imageDataSize);
        bw.Write(6 + 16);     // offset (header=6 + one entry=16)

        // BITMAPINFOHEADER
        bw.Write(bmpHeaderSize);
        bw.Write(size);
        bw.Write(size * 2);   // height doubled for ICO (XOR + AND)
        bw.Write((short)1);   // planes
        bw.Write((short)32);  // bpp
        bw.Write(0);          // compression
        bw.Write(pixels.Length + andMaskSize);
        bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);

        // Pixel data (bottom-up row order)
        for (int y = size - 1; y >= 0; y--)
            bw.Write(pixels, y * stride, stride);

        // AND mask (all zeros = fully visible, alpha handles transparency)
        bw.Write(new byte[andMaskSize]);
    }
}
