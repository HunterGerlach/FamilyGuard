using System.Runtime.InteropServices;
using FamilyGuard.Application.Ports.Input;

namespace FamilyGuard.Infrastructure.Platform.Windows;

public sealed class Win32PresenceDetector : IPresenceDetector
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("kernel32.dll")]
    private static extern uint GetTickCount();

    public TimeSpan GetIdleTime()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };

        if (!GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        var idleMillis = GetTickCount() - info.dwTime;
        return TimeSpan.FromMilliseconds(idleMillis);
    }

    public bool IsControllerActive()
    {
        // Delegate to XInputControllerDetector for separation of concerns.
        // This method exists on IPresenceDetector for composition;
        // the agent wires a composite detector that checks both.
        return false;
    }
}
