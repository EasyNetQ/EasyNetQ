using EasyNetQ.Internals;
using EasyNetQ.Tests.Mocking;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_responder_is_cancelled : IDisposable
{
    private readonly MockBuilder mockBuilder;

    public When_a_responder_is_cancelled()
    {
        mockBuilder = new MockBuilder();

        using var cde = new AsyncCountdownEvent(1);

        using var responder = mockBuilder.Rpc.Respond<RpcRequest, RpcResponse>(
            async (_, ct) =>
            {
                cde.Decrement();
                await Task.Delay(-1, ct);
                return new RpcResponse();
            },
            _ => { }
        );

        var deliverTask = DeliverMessageAsync(new RpcRequest());
        cde.WaitAsync().GetAwaiter().GetResult();

        deliverTask.GetAwaiter().GetResult();
    }

    public virtual void Dispose() => mockBuilder.Dispose();

    [Fact]
    public async Task Should_NACK_with_requeue()
    {
        await mockBuilder.Channels[2].Received().BasicNackAsync(0, false, true);
    }

    private async Task DeliverMessageAsync(RpcRequest request)
    {
        var properties = new BasicProperties
        {
            Type = mockBuilder.TypeNameSerializer.Serialize(typeof(RpcRequest)),
            CorrelationId = "the_correlation_id",
            ReplyTo = mockBuilder.Conventions.RpcReturnQueueNamingConvention(typeof(RpcResponse))
        };

        using (var serializedMessage = mockBuilder.Serializer.MessageToBytes(typeof(RpcRequest), request))
        {
            await mockBuilder.Consumers[0].HandleBasicDeliverAsync(
                "consumer tag",
                0,
                false,
                "the_exchange",
                "the_routing_key",
                properties,
                serializedMessage.Memory
            );
        }
    }

    private sealed record RpcRequest;

    private sealed record RpcResponse;
}
