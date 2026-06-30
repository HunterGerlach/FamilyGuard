namespace FamilyGuard.Infrastructure.Messaging;

/// <summary>
/// Bidirectional message channel between service and agent processes.
/// Enterprise Integration Pattern: Message Channel.
/// </summary>
public interface IMessageChannel : IAsyncDisposable
{
    Task SendAsync(ChannelMessage message, CancellationToken ct = default);
    Task<ChannelMessage?> ReceiveAsync(CancellationToken ct = default);
    bool IsConnected { get; }
}

public sealed class ChannelMessage
{
    public string Type { get; }
    public string? Payload { get; }
    public DateTimeOffset Timestamp { get; }

    public ChannelMessage(string type, string? payload = null)
    {
        Type = type;
        Payload = payload;
        Timestamp = DateTimeOffset.UtcNow;
    }
}
