using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using NSubstitute;

namespace EasyNetQ.Tests.ConsumeTests
{
    public abstract class ConsumerTestBase : IDisposable
    {
        protected MockBuilder MockBuilder;
        protected IConsumerErrorStrategy ConsumerErrorStrategy;
        protected const string ConsumerTag = "the_consumer_tag";
        protected byte[] DeliveredMessageBody;
        protected MessageProperties DeliveredMessageProperties;
        protected MessageReceivedInfo DeliveredMessageInfo;
        protected bool ConsumerWasInvoked;
        protected CancellationTokenSource Cancellation;

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

        protected void StartConsumer(Action<byte[], MessageProperties, MessageReceivedInfo> handler)
        {
            ConsumerWasInvoked = false;
            var queue = new Queue("my_queue", false);
            MockBuilder.Bus.Advanced.Consume(queue, (body, properties, messageInfo) => Task.Run(() =>
                {
                    DeliveredMessageBody = body;
                    DeliveredMessageProperties = properties;
                    DeliveredMessageInfo = messageInfo;

                    handler(body, properties, messageInfo);
                    ConsumerWasInvoked = true;
                }, Cancellation.Token));
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
            
            if(!waiter.Wait(5000))
            {
                throw new TimeoutException();
            }
        }
    }
}