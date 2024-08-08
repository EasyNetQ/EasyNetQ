using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests.ProducerTests;

public class When_a_request_is_sent_but_an_exception_is_thrown_by_responder : IDisposable
{
    private readonly MockBuilder mockBuilder;
    private readonly TestRequestMessage requestMessage;
    private readonly string correlationId;

    public When_a_request_is_sent_but_an_exception_is_thrown_by_responder()
    {
        correlationId = Guid.NewGuid().ToString();
        mockBuilder = new MockBuilder(
            c => c.AddSingleton<ICorrelationIdGenerationStrategy>(
                _ => new StaticCorrelationIdGenerationStrategy(correlationId)
            )
        );

        requestMessage = new TestRequestMessage();
    }

    public void Dispose()
    {
        mockBuilder.Dispose();
    }

    [Fact]
    public async Task Should_throw_an_EasyNetQResponderException()
    {
        await Assert.ThrowsAsync<EasyNetQResponderException>(async () =>
        {
            var waiter = new CountdownEvent(2);

            mockBuilder.EventBus.Subscribe((in PublishedMessageEvent _) => waiter.Signal());
            mockBuilder.EventBus.Subscribe((in StartConsumingSucceededEvent _) => waiter.Signal());

            var task = mockBuilder.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(requestMessage);
            if (!waiter.Wait(5000))
                throw new TimeoutException();

            DeliverMessage(null).GetAwaiter().GetResult();
            await task;
        });
    }

    [Fact]
    public async Task Should_throw_an_EasyNetQResponderException_with_a_specific_exception_message()
    {
        await Assert.ThrowsAsync<EasyNetQResponderException>(async () =>
        {
            var waiter = new CountdownEvent(2);

            mockBuilder.EventBus.Subscribe((in PublishedMessageEvent _) => waiter.Signal());
            mockBuilder.EventBus.Subscribe((in StartConsumingSucceededEvent _) => waiter.Signal());

            var task = mockBuilder.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(requestMessage);
            if (!waiter.Wait(5000))
                throw new TimeoutException();

            DeliverMessage("Why you are so bad with me?").GetAwaiter().GetResult();

            await task;
        }); // ,"Why you are so bad with me?"
    }

    private async Task DeliverMessage(string exceptionMessage)
    {
        var properties = new BasicProperties
        {
            Type = "EasyNetQ.Tests.TestResponseMessage, EasyNetQ.Tests",
            CorrelationId = correlationId,
            Headers = new Dictionary<string, object>
            {
                { "IsFaulted", true }
            }
        };

        if (exceptionMessage != null)
        {
            // strings are implicitly converted in byte[] from RabbitMQ client
            // but not converted back in string
            // check the source code in the class RabbitMQ.Client.Impl.WireFormatting
            properties.Headers.Add("ExceptionMessage", Encoding.UTF8.GetBytes(exceptionMessage));
        }

        var body = "{}"u8.ToArray();

        await mockBuilder.Consumers[0].HandleBasicDeliverAsync(
            "consumer_tag",
            0,
            false,
            "the_exchange",
            "the_routing_key",
            properties,
            body
        );
    }
}
