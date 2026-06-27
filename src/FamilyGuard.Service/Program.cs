using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Infrastructure.Persistence;
using FamilyGuard.Infrastructure.Platform.Windows;
using FamilyGuard.Service;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "FamilyGuard";
});

// Data path
var dataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "FamilyGuard");
Directory.CreateDirectory(dataPath);
var dbPath = Path.Combine(dataPath, "familyguard.db");

// Run migrations
var migrationConnection = new SqliteConnection($"Data Source={dbPath}");
migrationConnection.Open();
var runner = new MigrationRunner(migrationConnection);
runner.Run(Migrations.All);
migrationConnection.Close();
migrationConnection.Dispose();

// Infrastructure
builder.Services.AddSingleton<IEventStore>(new SqliteEventStore($"Data Source={dbPath}"));

// Platform — Windows session monitoring and agent lifecycle
var agentExePath = Path.Combine(AppContext.BaseDirectory, "FamilyGuard.Agent.exe");
builder.Services.AddSingleton<ISessionMonitor, WtsSessionMonitor>();
builder.Services.AddSingleton<IAgentLifecycleManager>(sp =>
    new WtsAgentLifecycleManager(agentExePath, sp.GetRequiredService<ILogger<WtsAgentLifecycleManager>>()));

// Service worker
builder.Services.AddHostedService<ServiceWorker>();

builder.Logging.AddEventLog(settings =>
{
    settings.SourceName = "FamilyGuard";
});

var host = builder.Build();
host.Run();
