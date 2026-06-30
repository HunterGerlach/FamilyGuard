using System.Drawing;
using System.Drawing.Drawing2D;
using FamilyGuard.UI.ViewModels;

namespace FamilyGuard.UI.Resources;

/// <summary>
/// Generates tray icons programmatically at runtime.
/// Avoids shipping separate .ico files for each state.
/// </summary>
public static class TrayIconGenerator
{
    private static readonly Dictionary<TrayIconState, Icon> Cache = new();

    public static Icon GetIcon(TrayIconState state)
    {
        if (Cache.TryGetValue(state, out var cached))
            return cached;

        var color = state switch
        {
            TrayIconState.Normal => Color.FromArgb(76, 175, 80),       // Green
            TrayIconState.Warning => Color.FromArgb(255, 193, 7),      // Yellow/Amber
            TrayIconState.ActionTaken => Color.FromArgb(244, 67, 54),  // Red/Orange
            TrayIconState.Disconnected => Color.FromArgb(158, 158, 158), // Gray
            _ => Color.Gray
        };

        var icon = CreateCircleIcon(color, 16);
        Cache[state] = icon;
        return icon;
    }

    private static Icon CreateCircleIcon(Color color, int size)
    {
        using var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, size - 2, size - 2);

        // Dark border for visibility
        using var pen = new Pen(Color.FromArgb(80, 0, 0, 0), 1);
        g.DrawEllipse(pen, 1, 1, size - 3, size - 3);

        return Icon.FromHandle(bitmap.GetHicon());
    }
}
