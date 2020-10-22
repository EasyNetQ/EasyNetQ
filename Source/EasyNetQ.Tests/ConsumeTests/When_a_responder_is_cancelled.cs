// ReSharper disable InconsistentNaming
using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
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

            mockBuilder.Rpc.Respond<RpcRequest, RpcResponse>(m =>
            {
                var tcs = new TaskCompletionSource<RpcResponse>();
                tcs.SetCanceled();
                return tcs.Task;
            });

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
            Assert.Equal("A task was canceled.", publishedMessage.Properties.Headers["ExceptionMessage"]);
            Assert.Equal(AckResult.Nack, ackEvent.AckResult);
        }

        private void DeliverMessage(RpcRequest request)
        {
            var properties = new BasicProperties
            {
                Type = typeNameSerializer.Serialize(typeof(RpcRequest)),
                CorrelationId = "the_correlation_id",
                ReplyTo = conventions.RpcReturnQueueNamingConvention(typeof(RpcResponse))
            };

            var body = serializer.MessageToBytes(typeof(RpcRequest), request);

            var waiter = new CountdownEvent(2);
            mockBuilder.EventBus.Subscribe<PublishedMessageEvent>(x =>
            {
                publishedMessage = x;
                waiter.Signal();
            });
            mockBuilder.EventBus.Subscribe<AckEvent>(x =>
            {
                ackEvent = x;
                waiter.Signal();
            });

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
                throw new TimeoutException();
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
