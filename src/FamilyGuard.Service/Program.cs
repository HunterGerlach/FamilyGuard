using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Application.UseCases;
using FamilyGuard.Infrastructure.Persistence;
using FamilyGuard.Infrastructure.Updates;
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
var connectionString = $"Data Source={dbPath}";

// Run migrations
using (var migrationConnection = new SqliteConnection(connectionString))
{
    migrationConnection.Open();
    new MigrationRunner(migrationConnection).Run(Migrations.All);
}

// Infrastructure — persistence
builder.Services.AddSingleton<IEventStore>(new SqliteEventStore(connectionString));
builder.Services.AddSingleton<ISettingsRepository>(new SqliteSettingsRepository(connectionString));

// Infrastructure — update system (12-Factor: URL from settings, not hardcoded)
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<IHashVerifier, Sha256HashVerifier>();
builder.Services.AddSingleton<IUpdateChecker>(sp =>
{
    var settings = sp.GetRequiredService<ISettingsRepository>().Load();
    return new HttpUpdateChecker(
        sp.GetRequiredService<HttpClient>(),
        settings.UpdateChannelUrl,
        sp.GetRequiredService<ILogger<HttpUpdateChecker>>());
});
builder.Services.AddSingleton<IUpdateInstaller>(sp =>
    new MsiUpdateInstaller(
        sp.GetRequiredService<HttpClient>(),
        Path.Combine(dataPath, "updates"),
        sp.GetRequiredService<ILogger<MsiUpdateInstaller>>()));

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
builder.Services.AddSingleton<ApplyUpdateUseCase>();

// Service worker
builder.Services.AddHostedService<ServiceWorker>();

var host = builder.Build();
host.Run();
