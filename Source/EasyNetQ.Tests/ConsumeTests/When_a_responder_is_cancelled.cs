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
        private readonly MockBuilder mockBuilder;
        private PublishedMessageEvent publishedMessage;
        private AckEvent ackEvent;

        private readonly IConventions conventions;
        private readonly ITypeNameSerializer typeNameSerializer;
        private readonly ISerializer serializer;

        public When_a_responder_is_cancelled()
        {
            mockBuilder = new MockBuilder();

            conventions = mockBuilder.Bus.Advanced.Conventions;
            typeNameSerializer = mockBuilder.Bus.Advanced.Container.Resolve<ITypeNameSerializer>();
            serializer = mockBuilder.Bus.Advanced.Container.Resolve<ISerializer>();

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
                Type = typeNameSerializer.Serialize(request.GetType()),
                CorrelationId = "the_correlation_id",
                ReplyTo = conventions.RpcReturnQueueNamingConvention()
            };

            var body = serializer.MessageToBytes(request);

            var waiter = new CountdownEvent(2);
            mockBuilder.EventBus.Subscribe<PublishedMessageEvent>(x => waiter.Signal());
            mockBuilder.EventBus.Subscribe<AckEvent>(x => waiter.Signal());

            mockBuilder.Consumers[0].HandleBasicDeliver(
                "consumer tag",
                0,
                false,
                "the_exchange",
                "the_routing_key",
                properties,
                body
                );

            if (!waiter.Wait(5000))
            {
                throw new TimeoutException();
            }
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
