using System.Collections.Generic;
using NAudio.CoreAudioApi;

namespace QqMusicRadio.Companion.Audio;

/// <summary>Enumerates active capture (recording) endpoints, e.g. VB-CABLE outputs.</summary>
public static class DeviceList
{
    public static List<string> GetCaptureDeviceNames()
    {
        var names = new List<string>();
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            foreach (var d in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
                names.Add(d.FriendlyName);
        }
        catch { /* enumeration failure = empty list */ }
        return names;
    }
}
