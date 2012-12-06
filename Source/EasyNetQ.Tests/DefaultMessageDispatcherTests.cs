using System;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class DefaultMessageDispatcherTests
    {
        [Test]
        public void Should_create_consumer_instance_and_dispatch_message()
        {
            var dispatcher = new DefaultMessageDispatcher();
            var message = new MyMessage();
            var consumedMessage = (MyMessage) null;
            
            MyMessageConsumer.ConsumedMessageFunc = m => consumedMessage = m;
            dispatcher.Dispatch<MyMessage, MyMessageConsumer>(message);

            Assert.AreSame(message, consumedMessage);
        }

        // Discovered by reflection over test assembly, do not remove.
        private class MyMessageConsumer : IConsume<MyMessage>
        {
            public static Action<MyMessage> ConsumedMessageFunc { get; set; }

            public void Consume(MyMessage message)
            {
                ConsumedMessageFunc(message);
            }
        }
    }
}