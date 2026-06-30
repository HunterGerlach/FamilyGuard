using Shouldly;
using Xunit;
using FamilyGuard.Infrastructure.Messaging;

namespace FamilyGuard.Infrastructure.Tests.Messaging;

public class InMemoryMessageChannelTests
{
    [Fact]
    public async Task SendAndReceive_RoundTrips()
    {
        var (server, client) = InMemoryMessageChannel.CreatePair();

        await server.SendAsync(new ChannelMessage("heartbeat", "ok"));
        var received = await client.ReceiveAsync();

        received.ShouldNotBeNull();
        received.Type.ShouldBe("heartbeat");
        received.Payload.ShouldBe("ok");
    }

    [Fact]
    public async Task BidirectionalCommunication_Works()
    {
        var (server, client) = InMemoryMessageChannel.CreatePair();

        await client.SendAsync(new ChannelMessage("status_request"));
        var request = await server.ReceiveAsync();
        request.ShouldNotBeNull();
        request.Type.ShouldBe("status_request");

        await server.SendAsync(new ChannelMessage("status_response", "running"));
        var response = await client.ReceiveAsync();
        response.ShouldNotBeNull();
        response.Type.ShouldBe("status_response");
    }

    [Fact]
    public async Task MultipleMessages_MaintainOrder()
    {
        var (server, client) = InMemoryMessageChannel.CreatePair();

        await server.SendAsync(new ChannelMessage("msg1"));
        await server.SendAsync(new ChannelMessage("msg2"));
        await server.SendAsync(new ChannelMessage("msg3"));

        var m1 = await client.ReceiveAsync();
        var m2 = await client.ReceiveAsync();
        var m3 = await client.ReceiveAsync();

        m1!.Type.ShouldBe("msg1");
        m2!.Type.ShouldBe("msg2");
        m3!.Type.ShouldBe("msg3");
    }

    [Fact]
    public async Task Receive_WithCancellation_ReturnNull()
    {
        var (_, client) = InMemoryMessageChannel.CreatePair();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var result = await client.ReceiveAsync(cts.Token);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task IsConnected_TrueUntilDisposed()
    {
        var (server, client) = InMemoryMessageChannel.CreatePair();

        server.IsConnected.ShouldBeTrue();
        client.IsConnected.ShouldBeTrue();

        await server.DisposeAsync();

        server.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public void Timestamp_AutoPopulated()
    {
        var before = DateTimeOffset.UtcNow;
        var msg = new ChannelMessage("test");
        var after = DateTimeOffset.UtcNow;

        msg.Timestamp.ShouldBeGreaterThanOrEqualTo(before);
        msg.Timestamp.ShouldBeLessThanOrEqualTo(after);
    }
}
