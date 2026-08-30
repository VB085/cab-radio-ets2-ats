using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QqMusicRadio.Companion.Server;

/// <summary>
/// Minimal hand-rolled HTTP server: any GET serves the MP3 stream,
/// "/" and "/status" serve a plain-text status page. Bound to loopback only.
/// </summary>
public sealed class HttpServer
{
    private readonly int _port;
    private readonly RingBuffer _buffer;
    private readonly Func<string> _statusText;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private int _clientCount;

    public HttpServer(int port, RingBuffer buffer, Func<string> statusText)
    {
        _port = port;
        _buffer = buffer;
        _statusText = statusText;
    }

    public int ClientCount => _clientCount;

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Loopback, _port);
        _listener.Start();
        _ = Task.Run(AcceptLoop, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { /* ignore */ }
    }

    private async Task AcceptLoop()
    {
        while (_cts is { IsCancellationRequested: false } && _listener is not null)
        {
            TcpClient client;
            try { client = await _listener.AcceptTcpClientAsync(_cts.Token); }
            catch { break; }
            _ = Task.Run(() => HandleClient(client));
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        Interlocked.Increment(ref _clientCount);
        try
        {
            client.NoDelay = true;
            await using var ns = client.GetStream();

            // Read the request head (crude but enough for FMOD and browsers).
            var head = new byte[4096];
            int total = 0;
            int headerEnd = -1;
            while (total < head.Length && headerEnd < 0)
            {
                int n = await ns.ReadAsync(head.AsMemory(total, head.Length - total));
                if (n <= 0) return;
                total += n;
                headerEnd = FindHeaderEnd(head, total);
            }
            if (headerEnd < 0) return;

            string requestLine = Encoding.ASCII.GetString(head, 0, Math.Min(total, headerEnd)).Split("\r\n")[0];
            string[] parts = requestLine.Split(' ');
            string path = parts.Length > 1 ? parts[1] : "/";

            if (path is "/" or "/status")
            {
                var body = Encoding.UTF8.GetBytes(_statusText());
                var headers = Encoding.ASCII.GetBytes(
                    "HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: " +
                    body.Length + "\r\nConnection: close\r\n\r\n");
                await ns.WriteAsync(headers);
                await ns.WriteAsync(body);
                return;
            }

            var resp = Encoding.ASCII.GetBytes(
                "HTTP/1.1 200 OK\r\nContent-Type: audio/mpeg\r\nCache-Control: no-cache\r\nConnection: close\r\n\r\n");
            await ns.WriteAsync(resp);

            var reader = _buffer.CreateReader();
            var chunk = new byte[16384];
            while (_cts is { IsCancellationRequested: false })
            {
                int n = reader.Read(chunk, 0, chunk.Length);
                if (n == 0) break;
                await ns.WriteAsync(chunk.AsMemory(0, n));
            }
        }
        catch { /* client disconnected - normal for streams */ }
        finally
        {
            client.Dispose();
            Interlocked.Decrement(ref _clientCount);
        }
    }

    private static int FindHeaderEnd(byte[] buf, int len)
    {
        for (int i = 3; i < len; i++)
            if (buf[i - 3] == '\r' && buf[i - 2] == '\n' && buf[i - 1] == '\r' && buf[i] == '\n')
                return i + 1;
        return -1;
    }
}
