using System;
using System.IO;
using NAudio.Lame;
using NAudio.Wave;

namespace QqMusicRadio.Companion.Audio;

/// <summary>Feeds 44.1 kHz 16-bit stereo PCM in, MP3 frames come out of the sink.</summary>
public interface IMp3Encoder : IDisposable
{
    void Write(byte[] pcm, int offset, int count);
}

public sealed class LameMp3Encoder : IMp3Encoder
{
    private readonly LameMP3FileWriter _writer;

    public LameMp3Encoder(Stream sink, int bitrate, int sampleRate = 44100, int channels = 2)
    {
        _writer = new LameMP3FileWriter(sink, new WaveFormat(sampleRate, 16, channels), bitrate);
    }

    public void Write(byte[] pcm, int offset, int count) => _writer.Write(pcm, offset, count);

    public void Dispose() => _writer.Dispose();
}

/// <summary>
/// Pre-encodes digital silence into a loop of self-contained CBR MP3 frames.
/// Used while capture is missing or stale so the game radio never disconnects.
/// </summary>
public static class SilenceBuilder
{
    public static byte[] Build(int bitrate, int seconds = 30, int sampleRate = 44100, int channels = 2)
    {
        using var ms = new MemoryStream();
        using var writer = new LameMP3FileWriter(ms, new WaveFormat(sampleRate, 16, channels), bitrate);
        var zeros = new byte[sampleRate * 2 * channels]; // one second of PCM silence
        for (int i = 0; i < seconds; i++)
            writer.Write(zeros, 0, zeros.Length);
        writer.Flush();
        return ms.ToArray();
    }
}
