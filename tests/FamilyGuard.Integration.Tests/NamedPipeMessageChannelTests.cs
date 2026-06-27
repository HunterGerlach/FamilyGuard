using Shouldly;
using Xunit;
using FamilyGuard.Infrastructure.Messaging;

namespace FamilyGuard.Integration.Tests;

[Trait("Category", "Windows")]
public class NamedPipeMessageChannelTests
{
    [Fact]
    public async Task InMemoryChannel_CrossProcessSimulation_Works()
    {
        // Verify InMemoryMessageChannel works as expected before real named pipes
        var (server, client) = InMemoryMessageChannel.CreatePair();

        await server.SendAsync(new ChannelMessage("heartbeat", "agent-1"));
        var received = await client.ReceiveAsync();

        received.ShouldNotBeNull();
        received.Type.ShouldBe("heartbeat");
        received.Payload.ShouldBe("agent-1");

        await server.DisposeAsync();
        await client.DisposeAsync();
    }
}
