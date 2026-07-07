using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PrivateListings.E2ETests.Support;

public sealed class FakeSmtpServer : IAsyncDisposable
{
    private readonly ConcurrentQueue<ReceivedEmail> _messages = new();
    private readonly List<Task> _clientTasks = [];
    private readonly object _clientTasksLock = new();
    private CancellationTokenSource? _stopping;
    private TcpListener? _listener;
    private Task? _acceptLoop;

    public int Port { get; private set; }

    public IReadOnlyList<ReceivedEmail> Messages => _messages.ToArray();

    public void Start()
    {
        if (_listener is not null)
        {
            return;
        }

        _stopping = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_stopping.Token));
    }

    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }

    public async Task<ReceivedEmail> WaitForMessageAsync(
        Func<ReceivedEmail, bool> predicate,
        TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            var message = Messages.FirstOrDefault(predicate);
            if (message is not null)
            {
                return message;
            }

            await Task.Delay(100).ConfigureAwait(false);
        }

        throw new TimeoutException("Aucun email SMTP de test ne correspond au prédicat attendu.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_stopping is not null)
        {
            await _stopping.CancelAsync().ConfigureAwait(false);
        }

        _listener?.Stop();

        if (_acceptLoop is not null)
        {
            try
            {
                await _acceptLoop.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        Task[] clients;
        lock (_clientTasksLock)
        {
            clients = [.. _clientTasks];
        }

        await Task.WhenAll(clients).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        _stopping?.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener!.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            var task = HandleClientAsync(client, cancellationToken);
            lock (_clientTasksLock)
            {
                _clientTasks.Add(task);
            }

            _ = task.ContinueWith(
                completed =>
                {
                    lock (_clientTasksLock)
                    {
                        _clientTasks.Remove(completed);
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        await using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        await using (var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\r\n"
        })
        {
            await writer.WriteLineAsync("220 privatelistings-e2e.local").ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    return;
                }

                if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("250-privatelistings-e2e.local").ConfigureAwait(false);
                    await writer.WriteLineAsync("250-8BITMIME").ConfigureAwait(false);
                    await writer.WriteLineAsync("250 OK").ConfigureAwait(false);
                    continue;
                }

                if (line.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith("RSET", StringComparison.OrdinalIgnoreCase)
                    || line.StartsWith("NOOP", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("250 OK").ConfigureAwait(false);
                    continue;
                }

                if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>").ConfigureAwait(false);
                    var body = await ReadDataAsync(reader, cancellationToken).ConfigureAwait(false);
                    _messages.Enqueue(new ReceivedEmail(body, DateTimeOffset.UtcNow));
                    await writer.WriteLineAsync("250 Queued").ConfigureAwait(false);
                    continue;
                }

                if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("221 Bye").ConfigureAwait(false);
                    return;
                }

                await writer.WriteLineAsync("250 OK").ConfigureAwait(false);
            }
        }
    }

    private static async Task<string> ReadDataAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null || line == ".")
            {
                break;
            }

            if (line.StartsWith("..", StringComparison.Ordinal))
            {
                line = line[1..];
            }

            builder.AppendLine(line);
        }

        return builder.ToString();
    }
}
