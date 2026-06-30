using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Infrastructure.Persistence;

namespace FamilyGuard.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository _settings;
    private readonly PinRateLimiter _rateLimiter;

    [ObservableProperty]
    private bool _isUnlocked;

    [ObservableProperty]
    private string _pinError = string.Empty;

    [ObservableProperty]
    private int _presenceTimeoutSeconds = 90;

    [ObservableProperty]
    private bool _showToastOnAutoMute = true;

    [ObservableProperty]
    private bool _showTrayWarning = true;

    public ObservableCollection<string> CoveredUsers { get; } = [];

    [ObservableProperty]
    private string _newUserName = string.Empty;

    public SettingsViewModel(ISettingsRepository settings, PinRateLimiter rateLimiter)
    {
        _settings = settings;
        _rateLimiter = rateLimiter;
    }

    public bool TryUnlock(string pin)
    {
        if (_rateLimiter.IsLocked)
        {
            PinError = $"Too many attempts. Try again after {_rateLimiter.LockoutExpiresAt:HH:mm:ss}.";
            return false;
        }

        if (!_settings.VerifyPin(pin))
        {
            _rateLimiter.RecordFailure();
            var remaining = _rateLimiter.RemainingAttempts;
            PinError = remaining > 0
                ? $"Incorrect PIN. {remaining} attempts remaining."
                : "Too many failed attempts. Settings locked temporarily.";
            return false;
        }

        _rateLimiter.RecordSuccess();
        IsUnlocked = true;
        PinError = string.Empty;
        LoadSettings();
        return true;
    }

    private void LoadSettings()
    {
        var settings = _settings.Load();
        PresenceTimeoutSeconds = settings.PresenceTimeoutSeconds;
        CoveredUsers.Clear();
        foreach (var user in settings.CoveredUsers)
            CoveredUsers.Add(user);
    }

    [RelayCommand]
    private void AddUser()
    {
        var name = NewUserName.Trim();
        if (!string.IsNullOrEmpty(name) && !CoveredUsers.Contains(name))
        {
            CoveredUsers.Add(name);
            NewUserName = string.Empty;
        }
    }

    [RelayCommand]
    private void RemoveUser(string user)
    {
        CoveredUsers.Remove(user);
    }

    [RelayCommand]
    private void Save()
    {
        if (!IsUnlocked) return;

        var settings = _settings.Load();
        settings.PresenceTimeoutSeconds = PresenceTimeoutSeconds;
        settings.CoveredUsers = [.. CoveredUsers];
        _settings.Save(settings);
    }

    public bool IsPinConfigured => !string.IsNullOrEmpty(_settings.Load().PinHash);

    public void SetInitialPin(string pin)
    {
        _settings.SetPin(pin);
    }
}
