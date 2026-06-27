using System.Threading.Channels;

namespace FamilyGuard.Infrastructure.Messaging;

/// <summary>
/// In-process message channel for testing and same-process scenarios.
/// Production uses NamedPipeMessageChannel for cross-process IPC.
/// </summary>
public sealed class InMemoryMessageChannel : IMessageChannel
{
    private readonly ChannelReader<ChannelMessage> _reader;
    private readonly ChannelWriter<ChannelMessage> _writer;
    private bool _disposed;

    public bool IsConnected => !_disposed;

    private InMemoryMessageChannel(
        ChannelReader<ChannelMessage> reader,
        ChannelWriter<ChannelMessage> writer)
    {
        _reader = reader;
        _writer = writer;
    }

    /// <summary>
    /// Creates a connected pair of channels (server, client).
    /// Messages sent on one are received on the other.
    /// </summary>
    public static (InMemoryMessageChannel Server, InMemoryMessageChannel Client) CreatePair()
    {
        var serverToClient = Channel.CreateUnbounded<ChannelMessage>();
        var clientToServer = Channel.CreateUnbounded<ChannelMessage>();

        var server = new InMemoryMessageChannel(clientToServer.Reader, serverToClient.Writer);
        var client = new InMemoryMessageChannel(serverToClient.Reader, clientToServer.Writer);

        return (server, client);
    }

    public async Task SendAsync(ChannelMessage message, CancellationToken ct = default)
    {
        await _writer.WriteAsync(message, ct);
    }

    public async Task<ChannelMessage?> ReceiveAsync(CancellationToken ct = default)
    {
        try
        {
            return await _reader.ReadAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (ChannelClosedException)
        {
            return null;
        }
    }

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        _writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}
