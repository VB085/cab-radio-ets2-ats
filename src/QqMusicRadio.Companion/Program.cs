using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QqMusicRadio.Companion.Audio;
using QqMusicRadio.Companion.Server;

namespace QqMusicRadio.Companion;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        using var mutex = new Mutex(true, "QqMusicRadio.Companion.SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("Cab Radio 已在运行。\nCab Radio is already running.", "Cab Radio · 驾驶室电台");
            return 1;
        }

        var config = Config.Load(args);
        Log.Info($"start v{typeof(Program).Assembly.GetName().Version}  device='{config.DeviceName}' port={config.Port} bitrate={config.Bitrate} automode={config.AutoMode}");

        // Capture -> LAME -> ring buffer -> HTTP
        using var ring = new RingBuffer(4 * 1024 * 1024);
        var sink = new RingBufferStream(ring);
        var enc = new EncoderHolder(sink, config.Bitrate);
        var capture = new CaptureManager(config.DeviceName);
        capture.Pcm16Available += (buf, off, n) => enc.WritePcm(buf, off, n);
        capture.CaptureStarted += enc.EnsureRate;

        var state = new CompanionState { AutoMode = config.AutoMode };

        HttpServer server = null!;
        server = new HttpServer(config.Port, ring, () => BuildStatus(config, state, capture, server));
        try
        {
            server.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法监听 127.0.0.1:{config.Port}：{ex.Message}\nCannot bind 127.0.0.1:{config.Port}.\n\n是否有其他推流程序（VLC 等）或 Cab Radio 实例在运行？\nIs another streamer (VLC etc.) or Cab Radio instance running?", "Cab Radio · 驾驶室电台");
            return 2;
        }
        Log.Info($"[http] listening http://127.0.0.1:{config.Port}/stream.mp3");

        using var cts = new CancellationTokenSource();
        var watcher = new GameWatcher();
        watcher.GamePresenceChanged += running =>
        {
            state.GameRunning = running;
            Log.Info($"[game] {(running ? "detected (ATS/ETS2 running)" : "closed")}");
        };

        // Silence pump: keeps the game radio connected while capture is missing/stale.
        var pumpTask = Task.Run(() => PumpLoop(cts.Token, ring, enc, capture, state, server, sink, config), cts.Token);

        // Controller: in auto mode, capture runs when the game is up or clients are listening.
        var ctrlTask = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                bool shouldRun = !state.AutoMode || state.GameRunning || server.ClientCount > 0;
                if (shouldRun) capture.EnsureRunning();
                else capture.StopCapture();
                try { await Task.Delay(2000, cts.Token); } catch (OperationCanceledException) { break; }
            }
        }, cts.Token);

        watcher.Start(cts.Token);

        using var tray = new TrayApp(config, state, capture, enc, () =>
            $"Cab Radio · {(state.AutoMode ? "auto" : "always")} · " +
            $"{(capture.IsCapturing ? (capture.IsFresh() ? "live" : "stale") : "off")} · " +
            $"{(state.GameRunning ? "game" : "idle")} · {server.ClientCount} client(s)", config.Port);
        tray.Run(); // message loop; returns after Exit

        cts.Cancel();
        try { Task.WaitAll(new[] { pumpTask, ctrlTask }, 3000); } catch { /* ignore */ }
        server.Stop();
        capture.Dispose();
        enc.Dispose();
        Log.Info("stopped");
        return 0;
    }

    private static void PumpLoop(
        CancellationToken ct, RingBuffer ring, EncoderHolder enc,
        CaptureManager capture, CompanionState state, HttpServer server,
        RingBufferStream sink, Config config)
    {
        int silenceOffset = 0;
        const int silenceChunk = 4096;
        int bytesPerSec = config.Bitrate * 1000 / 8;
        int silenceSleepMs = Math.Max(20, silenceChunk * 1000 / bytesPerSec);
        int tick = 0;

        while (!ct.IsCancellationRequested)
        {
            bool fresh = capture.IsCapturing && capture.IsFresh();
            state.CaptureFresh = fresh;

            if (fresh)
            {
                Thread.Sleep(50); // encoding happens on the capture callback thread
            }
            else
            {
                var loop = enc.SilenceLoop;
                if (loop.Length > 0)
                {
                    int n = Math.Min(silenceChunk, loop.Length - silenceOffset);
                    ring.Write(loop, silenceOffset, n);
                    silenceOffset = (silenceOffset + n) % loop.Length;
                    Thread.Sleep(silenceSleepMs);
                }
                else
                {
                    Thread.Sleep(200);
                }
            }

            if (++tick % 200 == 0)
                Log.Info($"[status] capture={(fresh ? "live" : "silence")} mp3KB={sink.BytesWritten / 1024} clients={server.ClientCount} game={state.GameRunning}");
        }
    }

    private static string BuildStatus(Config config, CompanionState state, CaptureManager capture, HttpServer server) =>
        "Cab Radio · 驾驶室电台\n" +
        $"stream 流地址: http://127.0.0.1:{config.Port}/stream.mp3\n" +
        $"bitrate 码率: {config.Bitrate} kbps\n" +
        $"mode 模式: {(state.AutoMode ? "auto / 自动 (follow game/clients 跟随游戏/客户端)" : "always / 常开")}\n" +
        $"game 游戏: {(state.GameRunning ? "running 运行中" : "not running 未运行")}\n" +
        $"capture 采集: {(!capture.IsCapturing ? "stopped 已停止" : capture.IsFresh() ? "live 实时" : "stale (silence) 断流(静音)")}\n" +
        $"device 设备: {(capture.DeviceName.Length > 0 ? capture.DeviceName : "-")}\n" +
        $"clients 客户端: {server.ClientCount}\n";
}
