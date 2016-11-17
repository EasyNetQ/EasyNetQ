// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    public class PublishSubscribeWithTopicsTests : IDisposable
    {
        private IBus bus;

        public PublishSubscribeWithTopicsTests()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        public void Dispose()
        {
            if(bus != null) bus.Dispose();
        }

        private MyMessage CreateMessage()
        {
            return new MyMessage { Text = "Hello! " + Guid.NewGuid().ToString().Substring(0, 5) };
        }

        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Publish_some_messages_with_topics()
        {
            bus.Publish(CreateMessage(), "X.A");
            bus.Publish(CreateMessage(), "X.B");
            bus.Publish(CreateMessage(), "Y.A");
        }

        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Subscribe_to_messages_with_topics()
        {
            var countdownEvent = new CountdownEvent(7);

            bus.Subscribe<MyMessage>("id1", msg =>
            {
                Console.WriteLine("I Get X: {0}", msg.Text);
                countdownEvent.Signal();
            }, x => x.WithTopic("X.*"));

            bus.Subscribe<MyMessage>("id2", msg =>
            {
                Console.WriteLine("I Get A: {0}", msg.Text);
                countdownEvent.Signal();
            }, x => x.WithTopic("*.A"));

            bus.Subscribe<MyMessage>("id3", msg =>
            {
                Console.WriteLine("I Get All: {0}", msg.Text);
                countdownEvent.Signal();
            });

            countdownEvent.Wait(1000);
        }

        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_subscribe_to_multiple_topic_strings()
        {
            var countdownEvent = new CountdownEvent(7);
            bus.Subscribe<MyMessage>("id4", msg =>
            {
                Console.WriteLine("I Get Y or B: {0}", msg.Text);
                countdownEvent.Signal();
            }, x => x.WithTopic("Y.*").WithTopic("*.B"));

            countdownEvent.Wait(500);
        }

        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Publish_a_messages_without_a_topic()
        {
            bus.Publish(CreateMessage());
        }
    }
}

// ReSharper restore InconsistentNaming