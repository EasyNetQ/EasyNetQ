using System.Text;
using EasyNetQ.Tests.Mocking;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_a_message_is_received : IDisposable
{
    private readonly MockBuilder mockBuilder;
    private MyMessage deliveredMyMessage;
    private MyOtherMessage deliveredMyOtherMessage;

    public When_a_message_is_received()
    {
        mockBuilder = new MockBuilder();

#pragma warning disable IDISP004
        mockBuilder.SendReceive.ReceiveAsync("the_queue", x => x
#pragma warning restore IDISP004
           .Add<MyMessage>(message => deliveredMyMessage = message)
           .Add<MyOtherMessage>(message => deliveredMyOtherMessage = message));

        DeliverMessageAsync("{ Text: \"Hello World :)\" }", "EasyNetQ.Tests.MyMessage, EasyNetQ.Tests").GetAwaiter().GetResult();
        DeliverMessageAsync("{ Text: \"Goodbye Cruel World!\" }", "EasyNetQ.Tests.MyOtherMessage, EasyNetQ.Tests").GetAwaiter().GetResult();
        DeliverMessageAsync("{ Text: \"Shouldn't get this\" }", "EasyNetQ.Tests.Unknown, EasyNetQ.Tests").GetAwaiter().GetResult();
    }

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    [Fact]
    public void Should_deliver_MyMessage()
    {
        deliveredMyMessage.Should().NotBeNull();
        deliveredMyMessage.Text.Should().Be("Hello World :)");
    }

    [Fact]
    public void Should_deliver_MyOtherMessage()
    {
        deliveredMyOtherMessage.Should().NotBeNull();
        deliveredMyOtherMessage.Text.Should().Be("Goodbye Cruel World!");
    }

    private async Task DeliverMessageAsync(string message, string type)
    {
        var properties = new BasicProperties
        {
            Type = type,
            CorrelationId = "the_correlation_id"
        };
        var body = Encoding.UTF8.GetBytes(message);

        await mockBuilder.Consumers[0].HandleBasicDeliverAsync(
            "consumer tag",
            0,
            false,
            "the_exchange",
            "the_routing_key",
            properties,
            body
        );
    }
}
