using System;
using System.Threading;

namespace QqMusicRadio.Companion.Server;

/// <summary>
/// Single-writer, multi-reader byte ring buffer. Readers keep independent cursors;
/// a reader that falls behind by more than the buffer capacity skips ahead instead
/// of blocking the writer (live radio semantics: drop, don't lag).
/// </summary>
public sealed class RingBuffer : IDisposable
{
    private readonly byte[] _buf;
    private readonly object _lock = new();
    private long _writePos;
    private bool _closed;

    public RingBuffer(int capacityBytes) => _buf = new byte[capacityBytes];

    public void Dispose() => Close();

    public void Write(byte[] data, int offset, int count)
    {
        lock (_lock)
        {
            for (int i = 0; i < count; i++)
            {
                _buf[(int)(_writePos % _buf.Length)] = data[offset + i];
                _writePos++;
            }
            Monitor.PulseAll(_lock);
        }
    }

    public void Close()
    {
        lock (_lock)
        {
            _closed = true;
            Monitor.PulseAll(_lock);
        }
    }

    public RingBufferReader CreateReader() => new(this);

    public sealed class RingBufferReader
    {
        private readonly RingBuffer _owner;
        private long _readPos;

        internal RingBufferReader(RingBuffer owner)
        {
            _owner = owner;
            // Join at the live edge: never replay backlog to a new client.
            lock (owner._lock)
            {
                _readPos = owner._writePos;
            }
        }

        /// <summary>Blocks until data is available; returns 0 when the buffer is closed.</summary>
        public int Read(byte[] dest, int offset, int count)
        {
            lock (_owner._lock)
            {
                while (!_owner._closed)
                {
                    long available = _owner._writePos - _readPos;
                    if (available > 0)
                    {
                        if (available > _owner._buf.Length)
                            _readPos = _owner._writePos - _owner._buf.Length; // skip ahead
                        int toRead = (int)Math.Min(count, Math.Min(available, _owner._buf.Length));
                        int start = (int)(_readPos % _owner._buf.Length);
                        int firstPart = Math.Min(toRead, _owner._buf.Length - start);
                        Buffer.BlockCopy(_owner._buf, start, dest, offset, firstPart);
                        if (firstPart < toRead)
                            Buffer.BlockCopy(_owner._buf, 0, dest, offset + firstPart, toRead - firstPart);
                        _readPos += toRead;
                        return toRead;
                    }
                    Monitor.Wait(_owner._lock);
                }
                return 0;
            }
        }
    }
}
