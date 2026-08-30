using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QqMusicRadio.Companion;

/// <summary>Polls for ATS/ETS2 processes and raises <see cref="GamePresenceChanged"/>.</summary>
public sealed class GameWatcher
{
    private static readonly string[] GameProcesses = { "eurotrucks2", "amtrucks" };

    public event Action<bool>? GamePresenceChanged;

    public bool IsGameRunning { get; private set; }

    public void Start(CancellationToken ct) => _ = Task.Run(() => LoopAsync(ct), ct);

    private async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            bool running = GameProcesses.Any(p => Process.GetProcessesByName(p).Length > 0);
            if (running != IsGameRunning)
            {
                IsGameRunning = running;
                GamePresenceChanged?.Invoke(running);
            }
            try { await Task.Delay(3000, ct); } catch (OperationCanceledException) { break; }
        }
    }
}
