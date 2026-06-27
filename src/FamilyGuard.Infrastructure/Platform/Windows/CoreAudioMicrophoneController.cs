using System.Runtime.InteropServices;
using FamilyGuard.Application.Ports.Output;
using FamilyGuard.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FamilyGuard.Infrastructure.Platform.Windows;

/// <summary>
/// Controls the Windows default communications microphone via Core Audio APIs.
/// Uses IMMDeviceEnumerator and IAudioEndpointVolume COM interfaces.
/// </summary>
public sealed class CoreAudioMicrophoneController : IMicrophoneController, IDisposable
{
    private readonly ILogger<CoreAudioMicrophoneController> _logger;

    // COM CLSIDs and IIDs
    private static readonly Guid CLSID_MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid IID_IMMDeviceEnumerator = new("A95664D2-9614-4F35-A746-DE8DB63617E6");
    private static readonly Guid IID_IAudioEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");

    // EDataFlow
    private const int eCapture = 1;

    // ERole
    private const int eCommunications = 2;

    // STGM_READ
    private const int STGM_READ = 0;

    // DEVICE_STATE_ACTIVE
    private const int DEVICE_STATE_ACTIVE = 0x00000001;

    public CoreAudioMicrophoneController(ILogger<CoreAudioMicrophoneController> logger)
    {
        _logger = logger;
    }

    public MicrophoneInfo? GetDefaultCommunicationsMicrophone()
    {
        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        IPropertyStore? props = null;

        try
        {
            enumerator = CreateDeviceEnumerator();
            if (enumerator is null)
                return null;

            var hr = enumerator.GetDefaultAudioEndpoint(eCapture, eCommunications, out device);
            if (hr != 0 || device is null)
            {
                _logger.LogDebug("No default communications capture device found (HRESULT: 0x{Hr:X8})", hr);
                return null;
            }

            device.GetId(out var deviceId);
            device.OpenPropertyStore(STGM_READ, out props);

            var friendlyName = GetDeviceFriendlyName(props);

            return new MicrophoneInfo(
                DeviceId: new DeviceId(deviceId ?? "unknown"),
                Name: friendlyName ?? "Unknown Microphone",
                IsCommunicationsDefault: true);
        }
        catch (COMException ex)
        {
            _logger.LogError(ex, "COM error accessing default communications microphone");
            return null;
        }
        finally
        {
            if (props is not null) Marshal.ReleaseComObject(props);
            if (device is not null) Marshal.ReleaseComObject(device);
            if (enumerator is not null) Marshal.ReleaseComObject(enumerator);
        }
    }

    public bool IsMuted()
    {
        return WithEndpointVolume((volume) =>
        {
            volume.GetMute(out var muted);
            return muted;
        }, defaultValue: true);
    }

    public void Mute()
    {
        WithEndpointVolume((volume) =>
        {
            var hr = volume.SetMute(true, Guid.Empty);
            if (hr != 0)
            {
                _logger.LogError("Failed to mute microphone (HRESULT: 0x{Hr:X8})", hr);
            }
            else
            {
                _logger.LogInformation("Microphone muted via Core Audio endpoint");
            }
            return hr == 0;
        }, defaultValue: false);
    }

    private T WithEndpointVolume<T>(Func<IAudioEndpointVolume, T> action, T defaultValue)
    {
        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        IAudioEndpointVolume? volume = null;

        try
        {
            enumerator = CreateDeviceEnumerator();
            if (enumerator is null)
                return defaultValue;

            var hr = enumerator.GetDefaultAudioEndpoint(eCapture, eCommunications, out device);
            if (hr != 0 || device is null)
                return defaultValue;

            hr = device.Activate(IID_IAudioEndpointVolume, 0x17 /* CLSCTX_ALL */, IntPtr.Zero, out var obj);
            if (hr != 0 || obj is null)
                return defaultValue;

            volume = (IAudioEndpointVolume)obj;
            return action(volume);
        }
        catch (COMException ex)
        {
            _logger.LogError(ex, "COM error in endpoint volume operation");
            return defaultValue;
        }
        finally
        {
            if (volume is not null) Marshal.ReleaseComObject(volume);
            if (device is not null) Marshal.ReleaseComObject(device);
            if (enumerator is not null) Marshal.ReleaseComObject(enumerator);
        }
    }

    private static IMMDeviceEnumerator? CreateDeviceEnumerator()
    {
        var type = Type.GetTypeFromCLSID(CLSID_MMDeviceEnumerator);
        if (type is null) return null;
        return Activator.CreateInstance(type) as IMMDeviceEnumerator;
    }

    private static string? GetDeviceFriendlyName(IPropertyStore? props)
    {
        if (props is null) return null;

        try
        {
            // PKEY_Device_FriendlyName = {a45c254e-df1c-4efd-8020-67d146a850e0}, 14
            var key = new PROPERTYKEY
            {
                fmtid = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
                pid = 14
            };

            props.GetValue(ref key, out var propVariant);
            var name = Marshal.PtrToStringUni(propVariant.Data.AsStringPtr);
            PropVariantClear(ref propVariant);
            return name;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PROPVARIANT pvar);

    public void Dispose()
    {
        // No persistent COM resources held
    }

    #region COM Interfaces

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IMMDeviceCollection ppDevices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);

        [PreserveSig]
        int GetDevice(string pwstrId, out IMMDevice ppDevice);

        [PreserveSig]
        int RegisterEndpointNotificationCallback(IntPtr pClient);

        [PreserveSig]
        int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(
            [MarshalAs(UnmanagedType.LPStruct)] Guid iid,
            int dwClsCtx,
            IntPtr pActivationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

        [PreserveSig]
        int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);

        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

        [PreserveSig]
        int GetState(out int pdwState);
    }

    [ComImport]
    [Guid("0657E251-DBEE-4D95-8E1F-AD7161677DBF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        [PreserveSig]
        int GetCount(out int pcDevices);

        [PreserveSig]
        int Item(int nDevice, out IMMDevice ppDevice);
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig]
        int GetCount(out int cProps);

        [PreserveSig]
        int GetAt(int iProp, out PROPERTYKEY pkey);

        [PreserveSig]
        int GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);

        [PreserveSig]
        int SetValue(ref PROPERTYKEY key, ref PROPVARIANT propvar);

        [PreserveSig]
        int Commit();
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig]
        int RegisterControlChangeNotify(IntPtr pNotify);

        [PreserveSig]
        int UnregisterControlChangeNotify(IntPtr pNotify);

        [PreserveSig]
        int GetChannelCount(out uint pnChannelCount);

        [PreserveSig]
        int SetMasterVolumeLevel(float fLevelDB, [MarshalAs(UnmanagedType.LPStruct)] Guid pguidEventContext);

        [PreserveSig]
        int SetMasterVolumeLevelScalar(float fLevel, [MarshalAs(UnmanagedType.LPStruct)] Guid pguidEventContext);

        [PreserveSig]
        int GetMasterVolumeLevel(out float pfLevelDB);

        [PreserveSig]
        int GetMasterVolumeLevelScalar(out float pfLevel);

        [PreserveSig]
        int SetChannelVolumeLevel(uint nChannel, float fLevelDB, [MarshalAs(UnmanagedType.LPStruct)] Guid pguidEventContext);

        [PreserveSig]
        int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, [MarshalAs(UnmanagedType.LPStruct)] Guid pguidEventContext);

        [PreserveSig]
        int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);

        [PreserveSig]
        int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);

        [PreserveSig]
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, [MarshalAs(UnmanagedType.LPStruct)] Guid pguidEventContext);

        [PreserveSig]
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);

        [PreserveSig]
        int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);

        [PreserveSig]
        int VolumeStepUp([MarshalAs(UnmanagedType.LPStruct)] Guid pguidEventContext);

        [PreserveSig]
        int VolumeStepDown([MarshalAs(UnmanagedType.LPStruct)] Guid pguidEventContext);

        [PreserveSig]
        int QueryHardwareSupport(out uint pdwHardwareSupportMask);

        [PreserveSig]
        int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROPERTYKEY
    {
        public Guid fmtid;
        public int pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROPVARIANT
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public PropVariantData Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariantData
    {
        [FieldOffset(0)] public IntPtr AsStringPtr;
        [FieldOffset(0)] public int AsInt32;
        [FieldOffset(0)] public long AsInt64;
    }

    #endregion
}
