using Microsoft.Win32;

namespace QqMusicRadio.Companion;

/// <summary>HKCU Run key toggle (no admin rights needed).</summary>
public static class Autostart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "QQMusicRadioCompanion";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) is string value && value.Length > 0;
    }

    public static void SetEnabled(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (enable)
        {
            key?.SetValue(ValueName, $"\"{Environment.ProcessPath}\"");
            Log.Info("[autostart] enabled");
        }
        else
        {
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
            Log.Info("[autostart] disabled");
        }
    }
}
