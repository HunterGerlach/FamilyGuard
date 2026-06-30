using FamilyGuard.Agent;
using FamilyGuard.Application.Policies;
using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Application.StateMachine;
using FamilyGuard.Application.UseCases;
using FamilyGuard.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Core application services
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<ISettingsRepository>().Load();
    return new PresenceStateMachine(settings.PresenceTimeoutSeconds, sp.GetRequiredService<TimeProvider>());
});
builder.Services.AddSingleton<IPolicyEngine, PolicyEngine>();

// Use cases
builder.Services.AddSingleton<EvaluatePresenceUseCase>();
builder.Services.AddSingleton<EvaluatePolicyUseCase>();
builder.Services.AddSingleton<MuteMicrophoneUseCase>();

// Infrastructure — persistence (cross-platform)
var dataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "FamilyGuard");
Directory.CreateDirectory(dataPath);

var dbPath = Path.Combine(dataPath, "familyguard.db");
builder.Services.AddSingleton<IEventStore>(new SqliteEventStore($"Data Source={dbPath}"));
builder.Services.AddSingleton<ISettingsRepository>(new SqliteSettingsRepository($"Data Source={dbPath}"));

// Infrastructure — Windows platform adapters (registered only on Windows)
if (OperatingSystem.IsWindows())
{
    AgentPlatformRegistration.RegisterWindowsServices(builder.Services);
}

// Hosted service
builder.Services.AddHostedService<AgentWorker>();

var host = builder.Build();
host.Run();
