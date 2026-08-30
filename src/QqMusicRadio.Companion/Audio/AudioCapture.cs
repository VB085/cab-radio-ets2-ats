using System;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace QqMusicRadio.Companion.Audio;

/// <summary>
/// WASAPI shared-mode capture of a recording endpoint (VB-CABLE "CABLE Output").
/// Converts float32 -> 16-bit PCM inline on the capture callback thread and raises
/// <see cref="Pcm16Available"/>. No intermediate buffering/resampling chain:
/// the device clock paces production, so over-production is impossible by design.
///
/// Note: CABLE Output is a *capture* endpoint, so plain WasapiCapture is correct —
/// WasapiLoopbackCapture (AUDCLNT_STREAMFLAGS_LOOPBACK) is only valid on render endpoints.
/// </summary>
public sealed class AudioCapture : IDisposable
{
    private readonly string _deviceFilter;
    private WasapiCapture? _capture;
    private volatile int _lastDataTick;
    private bool _disposed;

    public AudioCapture(string deviceFilter) => _deviceFilter = deviceFilter;

    public string DeviceFriendlyName { get; private set; } = "";

    /// <summary>16-bit PCM format of the raised data (device-native sample rate, stereo).</summary>
    public WaveFormat OutputFormat { get; private set; } = new(44100, 16, 2);

    /// <summary>Raised on the capture callback thread with 16-bit PCM data.</summary>
    public event Action<byte[], int, int>? Pcm16Available;

    public bool TryStart(out string? error)
    {
        error = null;
        _disposed = false;
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            MMDevice? device = null;
            foreach (var d in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                if (d.FriendlyName.Contains(_deviceFilter, StringComparison.OrdinalIgnoreCase))
                {
                    device = d;
                    break;
                }
            }

            if (device is null)
            {
                error = $"capture device containing '{_deviceFilter}' not found";
                return false;
            }

            DeviceFriendlyName = device.FriendlyName;
            _capture = new WasapiCapture(device);
            OutputFormat = new WaveFormat(_capture.WaveFormat.SampleRate, 16, 2);

            _capture.DataAvailable += OnDataAvailable;
            _capture.StartRecording();
            _lastDataTick = Environment.TickCount;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            Cleanup();
            return false;
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        _lastDataTick = Environment.TickCount;
        var handler = Pcm16Available;
        if (handler is null || _capture is null) return;

        // Convert device float32 -> 16-bit PCM inline.
        int sampleCount = e.BytesRecorded / 4;
        var pcm = new byte[sampleCount * 2];
        int outPos = 0;
        for (int i = 0; i < e.BytesRecorded; i += 4)
        {
            float f = BitConverter.ToSingle(e.Buffer, i);
            int s = (int)(f * 32767f);
            if (s > 32767) s = 32767;
            if (s < -32768) s = -32768;
            pcm[outPos++] = (byte)(s & 0xFF);
            pcm[outPos++] = (byte)((s >> 8) & 0xFF);
        }
        handler(pcm, 0, pcm.Length);
    }

    /// <summary>True when fresh capture data arrived within the last <paramref name="staleMs"/> ms.</summary>
    public bool IsFresh(int staleMs = 3000) =>
        _capture is not null && Environment.TickCount - _lastDataTick < staleMs;

    public void Dispose() => Cleanup();

    private void Cleanup()
    {
        if (_disposed) return;
        _disposed = true;
        try { _capture?.StopRecording(); } catch { /* ignore */ }
        _capture?.Dispose();
        _capture = null;
    }
}
