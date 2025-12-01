using EasyNetQ.Consumer;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_consume_is_called_with_auto_ack : ConsumerTestBase
{
    protected override async Task InitializeAsyncCore()
    {
#pragma warning disable IDISP004
        await StartConsumerAsync((_, _, _, _) => AckStrategies.AckAsync, true);
#pragma warning restore IDISP004
    }

    [Fact]
    public void Should_create_a_consumer()
    {
        MockBuilder.Consumers.Count.Should().Be(1);
    }

    [Fact]
    public void Should_create_a_channel_to_consume_on()
    {
        MockBuilder.Channels.Count.Should().Be(1);
    }

    [Fact]
    public async Task Should_invoke_basic_consume_on_channel()
    {
        await MockBuilder.Channels[0].Received().BasicConsumeAsync(
           Arg.Is("my_queue"),
           Arg.Is(true),
           Arg.Is(ConsumerTag),
           Arg.Is(true),
           Arg.Is(false),
           Arg.Any<IDictionary<string, object>>(),
           Arg.Is(MockBuilder.Consumers[0]),
           default
       );
    }
}
