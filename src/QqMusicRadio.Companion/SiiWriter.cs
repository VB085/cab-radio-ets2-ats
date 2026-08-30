using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace QqMusicRadio.Companion;

/// <summary>
/// Idempotently ensures the Cab Radio entry exists in both games' live_streams.sii.
/// Verified 2026-08: the game reads its radio list SOLELY from this file — mod defs
/// are ignored and the built-in fallback is just a small template. So the Companion
/// owns injection. Runs on startup and periodically; the game may rewrite the file
/// (template regeneration etc.), and this heals it back.
/// </summary>
public static class SiiWriter
{
    private static readonly string[] GameDirs =
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "American Truck Simulator"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Euro Truck Simulator 2"),
    };

    /// <summary>Returns the number of files written.</summary>
    public static int EnsureEntry(string url, string name = "Cab Radio", int bitrate = 128)
    {
        int written = 0;
        string entry = $"{url}|{name}|Music|CN|{bitrate}|1";
        foreach (var dir in GameDirs)
        {
            string path = Path.Combine(dir, "live_streams.sii");
            if (!File.Exists(path)) continue; // game never ran yet

            string content;
            try { content = File.ReadAllText(path); }
            catch { continue; } // file locked (game running) — retry next cycle

            if (content.Contains(url))
            {
                continue; // already present
            }

            // Replace-in-place any previous localhost entry (port may have changed).
            if (Regex.IsMatch(content, @"stream_data\[\d+\]:\s*""[^""]*127\.0\.0\.1:\d+[^""]*"""))
            {
                content = Regex.Replace(content,
                    @"stream_data\[(\d+)\]:\s*""[^""]*127\.0\.0\.1:\d+[^""]*""",
                    m => $"stream_data[{m.Groups[1].Value}]: \"{entry}\"");
            }
            else
            {
                // Append after the last entry (inside the live_stream_def block) and bump the count.
                int newIndex = 0;
                foreach (Match m in Regex.Matches(content, @"stream_data\[(\d+)\]"))
                    newIndex = Math.Max(newIndex, int.Parse(m.Groups[1].Value) + 1);

                var countMatch = Regex.Match(content, @"stream_data:\s*(\d+)");
                if (countMatch.Success)
                {
                    int newCount = int.Parse(countMatch.Groups[1].Value) + 1;
                    content = new Regex(@"stream_data:\s*(\d+)").Replace(content, $"stream_data: {newCount}", 1);
                }

                var entryMatches = Regex.Matches(content, @"stream_data\[\d+\]:\s*""[^""]*""\r?\n");
                if (entryMatches.Count == 0) continue; // no anchor — nothing to do
                var last = entryMatches[entryMatches.Count - 1];
                content = content.Insert(last.Index + last.Length, $" stream_data[{newIndex}]: \"{entry}\"\r\n");
            }

            try
            {
                File.WriteAllText(path, content, new UTF8Encoding(false));
            }
            catch { continue; }
            written++;
            Log.Info($"[sii] ensured entry in {Path.GetFileName(dir)}");
        }
        return written;
    }
}
