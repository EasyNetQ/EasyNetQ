// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using EasyNetQ.AutoSubscribe;
using Xunit;

namespace EasyNetQ.Tests
{
    public class DefaultMessageConsumerTests
    {
        [Fact]
        public void Should_create_consumer_instance_and_consume_message()
        {
            var consumer = new DefaultAutoSubscriberMessageDispatcher();
            var message = new MyMessage();
            var consumedMessage = (MyMessage)null;

            MyMessageConsumer.ConsumedMessageFunc = m => consumedMessage = m;
            consumer.Dispatch<MyMessage, MyMessageConsumer>(message);

            Assert.Same(message, consumedMessage);
        }

        // Discovered by reflection over test assembly, do not remove.
        private class MyMessageConsumer : IConsume<MyMessage>
        {
            public static Action<MyMessage> ConsumedMessageFunc { get; set; }

            public void Consume(MyMessage message, CancellationToken cancellationToken)
            {
                ConsumedMessageFunc(message);
            }
        }
    }
}

// ReSharper restore InconsistentNaming
