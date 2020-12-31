using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using NSubstitute;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ConsumeTests
{
    public abstract class ConsumerTestBase : IDisposable
    {
        protected const string ConsumerTag = "the_consumer_tag";
        protected const ulong DeliverTag = 10101;
        protected readonly CancellationTokenSource Cancellation;
        protected readonly IConsumerErrorStrategy ConsumerErrorStrategy;
        protected readonly MockBuilder MockBuilder;
        protected bool ConsumerWasInvoked;
        protected ReadOnlyMemory<byte> DeliveredMessageBody;
        protected MessageReceivedInfo DeliveredMessageInfo;
        protected MessageProperties DeliveredMessageProperties;
        protected byte[] OriginalBody;

        // populated when a message is delivered
        protected IBasicProperties OriginalProperties;

        public ConsumerTestBase()
        {
            Cancellation = new CancellationTokenSource();

            ConsumerErrorStrategy = Substitute.For<IConsumerErrorStrategy>();
            MockBuilder = new MockBuilder(x => x.Register(ConsumerErrorStrategy));
            AdditionalSetUp();
        }

        public void Dispose()
        {
            MockBuilder.Dispose();
        }

        protected abstract void AdditionalSetUp();

        protected void StartConsumer(Func<ReadOnlyMemory<byte>, MessageProperties, MessageReceivedInfo, AckStrategy> handler)
        {
            ConsumerWasInvoked = false;
            var queue = new Queue("my_queue", false);
            MockBuilder.Bus.Advanced.Consume(
                queue,
                (body, properties, messageInfo) =>
                {
                    return Task.Run(() =>
                    {
                        DeliveredMessageBody = body;
                        DeliveredMessageProperties = properties;
                        DeliveredMessageInfo = messageInfo;

                        var ackStrategy = handler(body, properties, messageInfo);
                        ConsumerWasInvoked = true;
                        return ackStrategy;
                    }, Cancellation.Token);
                },
                c => c.WithConsumerTag(ConsumerTag)
            );
        }

        protected void DeliverMessage()
        {
            OriginalProperties = new BasicProperties
            {
                Type = "the_message_type",
                CorrelationId = "the_correlation_id",
            };
            OriginalBody = Encoding.UTF8.GetBytes("Hello World");

            var waiter = new CountdownEvent(2);

            MockBuilder.EventBus.Subscribe<DeliveredMessageEvent>(x => waiter.Signal());
            MockBuilder.EventBus.Subscribe<AckEvent>(x => waiter.Signal());

            MockBuilder.Consumers[0].HandleBasicDeliver(
                ConsumerTag,
                DeliverTag,
                false,
                "the_exchange",
                "the_routing_key",
                OriginalProperties,
                OriginalBody
            );

            if (!waiter.Wait(5000))
            {
                throw new TimeoutException();
            }
        }
    }
}
