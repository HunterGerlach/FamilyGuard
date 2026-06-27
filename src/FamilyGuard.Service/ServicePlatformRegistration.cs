#if WINDOWS
using System.Runtime.Versioning;
using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Infrastructure.Platform.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Service;

[SupportedOSPlatform("windows")]
public static class ServicePlatformRegistration
{
    public static void RegisterWindowsServices(IServiceCollection services, string agentExePath)
    {
        services.AddSingleton<ISessionMonitor, WtsSessionMonitor>();
        services.AddSingleton<IAgentLifecycleManager>(sp =>
            new WtsAgentLifecycleManager(agentExePath, sp.GetRequiredService<ILogger<WtsAgentLifecycleManager>>()));
    }
}
#endif
