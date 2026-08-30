using System;
using System.IO;
using QqMusicRadio.Companion.Server;

namespace QqMusicRadio.Companion.Audio;

/// <summary>Write-only Stream adapter so the MP3 encoder can push straight into the ring buffer.</summary>
public sealed class RingBufferStream : Stream
{
    private readonly RingBuffer _buffer;
    private long _bytesWritten;

    public RingBufferStream(RingBuffer buffer) => _buffer = buffer;

    public long BytesWritten => Interlocked.Read(ref _bytesWritten);

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
        _buffer.Write(buffer, offset, count);
        Interlocked.Add(ref _bytesWritten, count);
    }
}
