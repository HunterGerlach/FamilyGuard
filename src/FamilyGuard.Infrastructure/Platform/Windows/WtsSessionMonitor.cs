using System.Runtime.InteropServices;
using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Domain.Enums;
using FamilyGuard.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Infrastructure.Platform.Windows;

public sealed class WtsSessionMonitor : ISessionMonitor
{
    private readonly ILogger<WtsSessionMonitor> _logger;

    private const int WTS_CURRENT_SERVER_HANDLE = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct WTS_SESSION_INFO
    {
        public int SessionId;
        public IntPtr pWinStationName;
        public WTS_CONNECTSTATE_CLASS State;
    }

    private enum WTS_CONNECTSTATE_CLASS
    {
        WTSActive,
        WTSConnected,
        WTSConnectQuery,
        WTSShadow,
        WTSDisconnected,
        WTSIdle,
        WTSListen,
        WTSReset,
        WTSDown,
        WTSInit
    }

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSEnumerateSessions(
        IntPtr hServer,
        int Reserved,
        int Version,
        out IntPtr ppSessionInfo,
        out int pCount);

    [DllImport("wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr pMemory);

    public WtsSessionMonitor(ILogger<WtsSessionMonitor> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<SessionId> GetActiveInteractiveSessions()
    {
        var sessions = new List<SessionId>();

        if (!WTSEnumerateSessions(IntPtr.Zero, 0, 1, out var pSessionInfo, out var count))
        {
            _logger.LogWarning("WTSEnumerateSessions failed");
            return sessions;
        }

        try
        {
            var structSize = Marshal.SizeOf<WTS_SESSION_INFO>();
            for (int i = 0; i < count; i++)
            {
                var ptr = pSessionInfo + (i * structSize);
                var info = Marshal.PtrToStructure<WTS_SESSION_INFO>(ptr);

                // Skip session 0 (services) and non-active sessions
                if (info.SessionId > 0 &&
                    info.State is WTS_CONNECTSTATE_CLASS.WTSActive or WTS_CONNECTSTATE_CLASS.WTSConnected)
                {
                    sessions.Add(new SessionId(info.SessionId));
                }
            }
        }
        finally
        {
            WTSFreeMemory(pSessionInfo);
        }

        return sessions;
    }

    public SessionState GetSessionState(SessionId sessionId)
    {
        if (!WTSEnumerateSessions(IntPtr.Zero, 0, 1, out var pSessionInfo, out var count))
            return SessionState.Unknown;

        try
        {
            var structSize = Marshal.SizeOf<WTS_SESSION_INFO>();
            for (int i = 0; i < count; i++)
            {
                var ptr = pSessionInfo + (i * structSize);
                var info = Marshal.PtrToStructure<WTS_SESSION_INFO>(ptr);

                if (info.SessionId == sessionId.Value)
                {
                    return info.State switch
                    {
                        WTS_CONNECTSTATE_CLASS.WTSActive => SessionState.Active,
                        WTS_CONNECTSTATE_CLASS.WTSConnected => SessionState.Active,
                        WTS_CONNECTSTATE_CLASS.WTSDisconnected => SessionState.Disconnected,
                        _ => SessionState.Unknown
                    };
                }
            }
        }
        finally
        {
            WTSFreeMemory(pSessionInfo);
        }

        return SessionState.Unknown;
    }
}
