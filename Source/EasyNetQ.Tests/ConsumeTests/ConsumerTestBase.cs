using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using NSubstitute;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Tests.ConsumeTests
{
    public abstract class ConsumerTestBase : IDisposable
    {
        protected readonly MockBuilder MockBuilder;
        protected readonly IConsumerErrorStrategy ConsumerErrorStrategy;
        protected const string ConsumerTag = "the_consumer_tag";
        protected byte[] DeliveredMessageBody;
        protected MessageProperties DeliveredMessageProperties;
        protected MessageReceivedInfo DeliveredMessageInfo;
        protected bool ConsumerWasInvoked;
        protected readonly CancellationTokenSource Cancellation;

        // populated when a message is delivered
        protected IBasicProperties OriginalProperties;
        protected byte[] OriginalBody;
        protected const ulong DeliverTag = 10101;

        public ConsumerTestBase()
        {
            Cancellation = new CancellationTokenSource();

            ConsumerErrorStrategy = Substitute.For<IConsumerErrorStrategy>();

            IConventions conventions = new Conventions(new DefaultTypeNameSerializer())
                {
                    ConsumerTagConvention = () => ConsumerTag
                };
            MockBuilder = new MockBuilder(x => x
                    .Register(conventions)
                    .Register(ConsumerErrorStrategy)
                );

            AdditionalSetUp();
        }

        public void Dispose()
        {
            MockBuilder.Bus.Dispose();
        }

        protected abstract void AdditionalSetUp();

        protected void StartConsumer(Func<byte[], MessageProperties, MessageReceivedInfo, AckStrategy> handler)
        {
            ConsumerWasInvoked = false;
            var queue = new Queue("my_queue", false);
            MockBuilder.Bus.Advanced.Consume(queue, (body, properties, messageInfo) =>
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
            });
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
