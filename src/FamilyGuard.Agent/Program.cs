using FamilyGuard.Agent;
using FamilyGuard.Application.Policies;
using FamilyGuard.Application.Ports.Input;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Application.StateMachine;
using FamilyGuard.Application.UseCases;
using FamilyGuard.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Data path
var dataPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "FamilyGuard");
Directory.CreateDirectory(dataPath);
var dbPath = Path.Combine(dataPath, "familyguard.db");
var connectionString = $"Data Source={dbPath}";

// Run migrations
using (var migrationConn = new SqliteConnection(connectionString))
{
    migrationConn.Open();
    new MigrationRunner(migrationConn).Run(Migrations.All);
}

// Infrastructure — persistence
builder.Services.AddSingleton<IEventStore>(new SqliteEventStore(connectionString));
builder.Services.AddSingleton<ISettingsRepository>(new SqliteSettingsRepository(connectionString));
builder.Services.AddSingleton<IPolicyRepository>(sp =>
{
    var conn = new SqliteConnection(connectionString);
    conn.Open();
    return new SqlitePolicyRepository(conn);
});

// Core application services
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton(sp =>
{
    var settings = sp.GetRequiredService<ISettingsRepository>().Load();
    return new PresenceStateMachine(settings.PresenceTimeoutSeconds, sp.GetRequiredService<TimeProvider>());
});
builder.Services.AddSingleton<IPolicyEngine, PolicyEngine>();
builder.Services.AddSingleton<PinRateLimiter>(sp =>
    new PinRateLimiter(sp.GetRequiredService<TimeProvider>()));

// Use cases
builder.Services.AddSingleton<EvaluatePresenceUseCase>();
builder.Services.AddSingleton<EvaluatePolicyUseCase>();
builder.Services.AddSingleton<MuteMicrophoneUseCase>();
builder.Services.AddSingleton<ManualMicControlUseCase>();

// Ensure default policy rules exist
builder.Services.AddSingleton(sp =>
{
    var repo = sp.GetRequiredService<IPolicyRepository>();
    repo.EnsureDefaultRules();
    return repo;
});

// Platform — Windows adapters
if (OperatingSystem.IsWindows())
{
    AgentPlatformRegistration.RegisterWindowsServices(builder.Services);
}

// Hosted service
builder.Services.AddHostedService<AgentWorker>();

var host = builder.Build();
host.Run();
