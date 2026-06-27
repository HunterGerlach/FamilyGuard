using FamilyGuard.Application.Ports.Input;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Infrastructure.Platform.Windows;

/// <summary>
/// Combines Win32 keyboard/mouse idle detection with XInput controller detection.
/// Controller input is treated equally to keyboard/mouse per spec.
/// </summary>
public sealed class CompositePresenceDetector : IPresenceDetector
{
    private readonly Win32PresenceDetector _win32;
    private readonly XInputControllerDetector _xinput;
    private readonly ILogger<CompositePresenceDetector> _logger;
    private bool _controllerWarningLogged;

    public CompositePresenceDetector(
        Win32PresenceDetector win32,
        XInputControllerDetector xinput,
        ILogger<CompositePresenceDetector> logger)
    {
        _win32 = win32;
        _xinput = xinput;
        _logger = logger;
    }

    public TimeSpan GetIdleTime() => _win32.GetIdleTime();

    public bool IsControllerActive()
    {
        try
        {
            return _xinput.HasNewInput();
        }
        catch (DllNotFoundException)
        {
            if (!_controllerWarningLogged)
            {
                _logger.LogWarning(
                    "XInput not available — controller detection disabled. " +
                    "Install the Xbox Accessories app or DirectX runtime.");
                _controllerWarningLogged = true;
            }
            return false;
        }
    }
}
