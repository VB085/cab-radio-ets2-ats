using System;

namespace QqMusicRadio.Companion.Audio;

/// <summary>
/// Owns the AudioCapture lifecycle: idempotent start (retries until the device
/// appears/frees up), explicit stop, hot-plug re-acquire via EnsureRunning.
/// </summary>
public sealed class CaptureManager : IDisposable
{
    private string _deviceFilter;
    private AudioCapture? _capture;

    public CaptureManager(string deviceFilter) => _deviceFilter = deviceFilter;

    /// <summary>Raised on the capture callback thread with 16-bit PCM data.</summary>
    public event Action<byte[], int, int>? Pcm16Available;

    /// <summary>Raised when a capture session starts, with the device-native sample rate.</summary>
    public event Action<int>? CaptureStarted;

    public string DeviceName { get; private set; } = "";

    public string? LastError { get; private set; }

    public bool IsCapturing => _capture is not null;

    /// <summary>Starts capture if not running; retries safely on failure.</summary>
    public void EnsureRunning()
    {
        if (_capture is not null) return;

        var cap = new AudioCapture(_deviceFilter);
        if (cap.TryStart(out string? error))
        {
            _capture = cap;
            DeviceName = cap.DeviceFriendlyName;
            LastError = null;
            cap.Pcm16Available += (buf, off, n) => Pcm16Available?.Invoke(buf, off, n);
            CaptureStarted?.Invoke(cap.OutputFormat.SampleRate);
            Log.Info($"[audio] capture started: '{DeviceName}' ({cap.OutputFormat})");
        }
        else
        {
            cap.Dispose();
            LastError = error;
            Log.Info($"[audio] capture unavailable: {error}");
        }
    }

    /// <summary>Switches the device filter; the running session stops and the next EnsureRunning re-acquires.</summary>
    public void SetDeviceFilter(string filter)
    {
        if (filter == _deviceFilter && _capture is not null) return;
        _deviceFilter = filter;
        StopCapture();
    }

    public void StopCapture()
    {
        if (_capture is null) return;
        Log.Info("[audio] capture stopped");
        _capture.Dispose();
        _capture = null;
    }

    /// <summary>True while the current session received data within the last few seconds.</summary>
    public bool IsFresh() => _capture?.IsFresh() ?? false;

    public void Dispose() => StopCapture();
}
