using System;

namespace QqMusicRadio.Companion.Audio;

/// <summary>
/// Owns the LAME encoder and the matching pre-encoded silence loop.
/// Rebuilds both if a capture session reports a different sample rate
/// (e.g. default 44.1 kHz at startup, real device later at 48 kHz).
/// </summary>
public sealed class EncoderHolder : IDisposable
{
    private readonly RingBufferStream _sink;
    private readonly object _lock = new();
    private LameMp3Encoder? _encoder;
    private int _bitrate;
    private int _rate = 44100;

    public EncoderHolder(RingBufferStream sink, int bitrate)
    {
        _sink = sink;
        _bitrate = bitrate;
        Rebuild(44100);
    }

    /// <summary>Pre-encoded silence loop at the current sample rate.</summary>
    public byte[] SilenceLoop { get; private set; } = Array.Empty<byte>();

    /// <summary>Rebuilds encoder + silence loop when the sample rate changes.</summary>
    public void EnsureRate(int sampleRate)
    {
        lock (_lock)
        {
            if (sampleRate != _rate) Rebuild(sampleRate);
        }
    }

    /// <summary>Switches bitrate; encoder and silence loop are rebuilt.</summary>
    public void SetBitrate(int bitrate)
    {
        lock (_lock)
        {
            if (bitrate == _bitrate) return;
            _bitrate = bitrate;
            Rebuild(_rate);
        }
    }

    public void WritePcm(byte[] buffer, int offset, int count)
    {
        lock (_lock)
        {
            _encoder?.Write(buffer, offset, count);
        }
    }

    private void Rebuild(int rate)
    {
        _encoder?.Dispose();
        _rate = rate;
        _encoder = new LameMp3Encoder(_sink, _bitrate, rate, 2);
        SilenceLoop = SilenceBuilder.Build(_bitrate, seconds: 30, rate, 2);
        Log.Info($"[audio] encoder ready: {rate}Hz / {_bitrate}kbps, silence loop {SilenceLoop.Length} bytes");
    }

    public void Dispose()
    {
        lock (_lock) _encoder?.Dispose();
    }
}
