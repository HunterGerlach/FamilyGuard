using FamilyGuard.Application.Ports.Output;

namespace FamilyGuard.Application.UseCases;

public sealed class CheckFirstRunUseCase
{
    private readonly ISettingsRepository _settings;
    private readonly IPolicyRepository _policyRepo;

    public CheckFirstRunUseCase(ISettingsRepository settings, IPolicyRepository policyRepo)
    {
        _settings = settings;
        _policyRepo = policyRepo;
    }

    public bool IsPinConfigured()
    {
        var settings = _settings.Load();
        return !string.IsNullOrEmpty(settings.PinHash);
    }

    public void SetupInitialPin(string pin)
    {
        _settings.SetPin(pin);
    }

    public void EnsureDefaults()
    {
        _policyRepo.EnsureDefaultRules();
    }
}
