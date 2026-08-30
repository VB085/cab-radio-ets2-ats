using System;
using System.IO;

namespace QqMusicRadio.Companion;

/// <summary>Append-only file logger (%APPDATA%\QQMusicRadio\companion.log).</summary>
public static class Log
{
    private static readonly object Lock = new();

    public static string FilePath => Path.Combine(Config.ConfigDir, "companion.log");

    public static void Info(string message)
    {
        lock (Lock)
        {
            try
            {
                Directory.CreateDirectory(Config.ConfigDir);
                File.AppendAllText(FilePath, $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}");
            }
            catch { /* logging must never crash the app */ }
        }
    }
}
