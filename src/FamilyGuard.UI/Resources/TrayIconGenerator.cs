using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FamilyGuard.UI.ViewModels;

namespace FamilyGuard.UI.Resources;

/// <summary>
/// Generates tray icons as .ico files on disk.
/// H.NotifyIcon requires: URI-backed ImageSource that resolves to
/// a valid ICO file (not PNG). We write ICO format with embedded PNG.
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
        RenderShieldToIco(color, 32, filePath);

        var image = new BitmapImage(new Uri(filePath, UriKind.Absolute));
        image.Freeze();
        Cache[state] = image;
        return image;
    }

    private static void RenderShieldToIco(Color fillColor, int size, string outputPath)
    {
        // Render the shield to a bitmap
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

        var renderBitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Bgra32);
        renderBitmap.Render(visual);

        // Get raw BGRA pixel data
        var stride = size * 4;
        var pixels = new byte[stride * size];
        renderBitmap.CopyPixels(pixels, stride, 0);

        // Write ICO file format (BMP-based, not PNG)
        using var fs = File.Create(outputPath);
        using var bw = new BinaryWriter(fs);

        // ICO header: reserved(2) + type=1(2) + count=1(2)
        bw.Write((short)0);      // reserved
        bw.Write((short)1);      // type: icon
        bw.Write((short)1);      // image count

        // ICO directory entry (16 bytes)
        var bmpInfoSize = 40;
        var pixelDataSize = pixels.Length;
        // AND mask: 1 bit per pixel, padded to 4 bytes per row
        var andMaskRowBytes = ((size + 31) / 32) * 4;
        var andMaskSize = andMaskRowBytes * size;
        var imageSize = bmpInfoSize + pixelDataSize + andMaskSize;

        bw.Write((byte)size);     // width (0 = 256)
        bw.Write((byte)size);     // height
        bw.Write((byte)0);        // color palette
        bw.Write((byte)0);        // reserved
        bw.Write((short)1);       // color planes
        bw.Write((short)32);      // bits per pixel
        bw.Write(imageSize);      // image data size
        bw.Write(6 + 16);         // offset to image data (header + 1 entry)

        // BITMAPINFOHEADER (40 bytes)
        bw.Write(bmpInfoSize);    // header size
        bw.Write(size);           // width
        bw.Write(size * 2);       // height (doubled for ICO: XOR + AND)
        bw.Write((short)1);       // planes
        bw.Write((short)32);      // bpp
        bw.Write(0);              // compression (none)
        bw.Write(pixelDataSize + andMaskSize); // image size
        bw.Write(0);              // x pixels per meter
        bw.Write(0);              // y pixels per meter
        bw.Write(0);              // colors used
        bw.Write(0);              // important colors

        // Write pixel data (bottom-up order for BMP)
        for (int y = size - 1; y >= 0; y--)
        {
            bw.Write(pixels, y * stride, stride);
        }

        // AND mask (all zeros = fully opaque, alpha channel handles transparency)
        bw.Write(new byte[andMaskSize]);
    }
}
