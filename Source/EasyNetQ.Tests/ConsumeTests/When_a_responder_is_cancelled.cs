using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client.Framing;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests
{

    public class When_a_responder_is_cancelled : IDisposable
    {
        private MockBuilder mockBuilder;
        private PublishedMessageEvent publishedMessage;
        private AckEvent ackEvent;

        private readonly IConventions Conventions;
        private readonly ITypeNameSerializer TypeNameSerializer;
        private readonly ISerializer Serializer;

        public When_a_responder_is_cancelled()
        {
            mockBuilder = new MockBuilder();

            Conventions = mockBuilder.Bus.Advanced.Conventions;
            TypeNameSerializer = mockBuilder.Bus.Advanced.Container.Resolve<ITypeNameSerializer>();
            Serializer = mockBuilder.Bus.Advanced.Container.Resolve<ISerializer>();

            mockBuilder.Bus.RespondAsync<RpcRequest, RpcResponse>(m =>
            {
                var taskSource = new TaskCompletionSource<RpcResponse>();
                taskSource.SetCanceled();
                return taskSource.Task;
            });

            mockBuilder.EventBus.Subscribe<PublishedMessageEvent>(x => publishedMessage = x);
            mockBuilder.EventBus.Subscribe<AckEvent>(x => ackEvent = x);

            DeliverMessage(new RpcRequest { Value = 42 });
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_ACK_with_faulted_response()
        {
            Assert.True((bool)publishedMessage.Properties.Headers["IsFaulted"]);
            Assert.Equal("The responder task was cancelled.", publishedMessage.Properties.Headers["ExceptionMessage"]);
            Assert.Equal(AckResult.Ack, ackEvent.AckResult);
        }

        private void DeliverMessage(RpcRequest request)
        {
            var properties = new BasicProperties
            {
                Type = TypeNameSerializer.Serialize(request.GetType()),
                CorrelationId = "the_correlation_id",
                ReplyTo = Conventions.RpcReturnQueueNamingConvention()
            };

            var body = Serializer.MessageToBytes(request);

            mockBuilder.Consumers[0].HandleBasicDeliver(
                "consumer tag",
                0,
                false,
                "the_exchange",
                "the_routing_key",
                properties,
                body
                );

            WaitForResponse();
        }

        private void WaitForResponse()
        {
            var waiter = new SemaphoreSlim(0, 2);
            mockBuilder.EventBus.Subscribe<PublishedMessageEvent>(x => waiter.Release());
            mockBuilder.EventBus.Subscribe<AckEvent>(x => waiter.Release());
            waiter.Wait(1000);
            waiter.Wait(1000);
        }

        private class RpcRequest
        {
            public int Value { get; set; }
        }

        private class RpcResponse
        {
        }
    }
}
