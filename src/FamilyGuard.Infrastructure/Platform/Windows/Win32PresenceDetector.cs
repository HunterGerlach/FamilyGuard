using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FamilyGuard.Application.Ports.Input;

namespace FamilyGuard.Infrastructure.Platform.Windows;

[SupportedOSPlatform("windows")]
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
    private static extern ulong GetTickCount64();

    public TimeSpan GetIdleTime()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };

        if (!GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        // GetLastInputInfo.dwTime is 32-bit (wraps at ~49.7 days).
        // GetTickCount64 is 64-bit. Mask to 32 bits for safe subtraction.
        var currentTick = (uint)(GetTickCount64() & 0xFFFFFFFF);
        var idleMillis = currentTick - info.dwTime;
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
