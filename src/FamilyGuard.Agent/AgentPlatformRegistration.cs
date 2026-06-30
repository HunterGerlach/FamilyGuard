#if WINDOWS
using System.Runtime.Versioning;
using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Infrastructure.Platform.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyGuard.Agent;

/// <summary>
/// Registers Windows-specific platform adapters.
/// Separated to keep Program.cs clean and enable conditional compilation.
/// </summary>
[SupportedOSPlatform("windows")]
public static class AgentPlatformRegistration
{
    public static void RegisterWindowsServices(IServiceCollection services)
    {
        services.AddSingleton<Win32PresenceDetector>();
        services.AddSingleton<XInputControllerDetector>();
        services.AddSingleton<IPresenceDetector, CompositePresenceDetector>();
        services.AddSingleton<IMicrophoneController, CoreAudioMicrophoneController>();
        services.AddSingleton<INotificationSender, TrayNotificationSender>();
        services.AddSingleton<ISystemEventMonitor, SystemEventMonitor>();
    }
}
#endif
