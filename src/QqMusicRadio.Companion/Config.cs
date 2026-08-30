using System;
using System.IO;
using System.Text.Json;

namespace QqMusicRadio.Companion;

public sealed class Config
{
    /// <summary>Substring match against capture device friendly names (VB-CABLE -> "CABLE").</summary>
    public string DeviceName { get; set; } = "CABLE";

    public int Port { get; set; } = 17890;

    public int Bitrate { get; set; } = 128;

    /// <summary>True = capture follows game presence / clients; false = always capture.</summary>
    public bool AutoMode { get; set; } = true;

    public static string ConfigDir
    {
        get
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CabRadio");
            // Migrate config/log from the pre-rename location.
            string oldDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QQMusicRadio");
            if (!Directory.Exists(dir) && Directory.Exists(oldDir))
            {
                try { Directory.Move(oldDir, dir); } catch { /* keep both if locked */ }
            }
            return dir;
        }
    }

    public static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    public static Config Load(string[] args)
    {
        var config = new Config();
        if (File.Exists(ConfigPath))
        {
            try
            {
                config = JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigPath)) ?? config;
            }
            catch
            {
                Console.WriteLine($"[config] failed to parse {ConfigPath}, using defaults");
            }
        }

        // CLI overrides win.
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--device" when i + 1 < args.Length:
                    config.DeviceName = args[++i];
                    break;
                case "--port" when i + 1 < args.Length && int.TryParse(args[++i], out int port):
                    config.Port = port;
                    break;
                case "--bitrate" when i + 1 < args.Length && int.TryParse(args[++i], out int bitrate):
                    config.Bitrate = Math.Clamp(bitrate, 64, 320);
                    break;
                case "--help":
                    Console.WriteLine("Usage: QqMusicRadio.Companion [--device CABLE] [--port 17890] [--bitrate 128]");
                    break;
            }
        }

        return config;
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
