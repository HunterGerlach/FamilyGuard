using System.Runtime.InteropServices;

namespace FamilyGuard.Infrastructure.Platform.Windows;

public sealed class XInputControllerDetector
{
    private const int ERROR_SUCCESS = 0;
    private const int XUSER_MAX_COUNT = 4;

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_GAMEPAD
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_STATE
    {
        public uint dwPacketNumber;
        public XINPUT_GAMEPAD Gamepad;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern int XInputGetState(int dwUserIndex, ref XINPUT_STATE pState);

    private readonly uint[] _lastPacketNumbers = new uint[XUSER_MAX_COUNT];

    /// <summary>
    /// Returns true if any connected controller has new input since the last check.
    /// </summary>
    public bool HasNewInput()
    {
        for (int i = 0; i < XUSER_MAX_COUNT; i++)
        {
            var state = new XINPUT_STATE();
            var result = XInputGetState(i, ref state);

            if (result != ERROR_SUCCESS)
                continue;

            if (state.dwPacketNumber != _lastPacketNumbers[i])
            {
                _lastPacketNumbers[i] = state.dwPacketNumber;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns true if any controller is currently connected.
    /// </summary>
    public bool IsAnyControllerConnected()
    {
        for (int i = 0; i < XUSER_MAX_COUNT; i++)
        {
            var state = new XINPUT_STATE();
            if (XInputGetState(i, ref state) == ERROR_SUCCESS)
                return true;
        }
        return false;
    }
}
