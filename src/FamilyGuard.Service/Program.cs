using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Application.UseCases;
using FamilyGuard.Infrastructure.Persistence;
using FamilyGuard.Service;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
using (var migrationConnection = new SqliteConnection($"Data Source={dbPath}"))
{
    migrationConnection.Open();
    new MigrationRunner(migrationConnection).Run(Migrations.All);
}

// Infrastructure
builder.Services.AddSingleton<IEventStore>(new SqliteEventStore($"Data Source={dbPath}"));

// Platform — Windows session monitoring and agent lifecycle
#if WINDOWS
if (OperatingSystem.IsWindows())
{
    var agentExePath = Path.Combine(AppContext.BaseDirectory, "FamilyGuard.Agent.exe");
    ServicePlatformRegistration.RegisterWindowsServices(builder.Services, agentExePath);
}
#endif

// Use cases
builder.Services.AddSingleton<SuperviseSessionsUseCase>();

// Service worker
builder.Services.AddHostedService<ServiceWorker>();

var host = builder.Build();
host.Run();
