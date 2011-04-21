// ReSharper disable InconsistentNaming
using System;
using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class PublishSubscribeTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateRabbitBus("appid", "localhost");
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_subscribe()
        {
            bus.Subscribe<MyMessage>(msg => Console.WriteLine(msg.Text));

            // allow time for messages to be consumed
            Thread.Sleep(100);

            Console.WriteLine("Stopped consuming");
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_publish()
        {
            bus.Publish(new MyMessage { Text = "Hello! " + Guid.NewGuid().ToString().Substring(0, 5) });
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_also_send_messages_to_second_subscriber()
        {
            var messageQueue2 = RabbitHutch.CreateRabbitBus("appid2", "localhost");
            messageQueue2.Subscribe<MyMessage>(msg => Console.WriteLine(msg.Text));

            // allow time for messages to be consumed
            Thread.Sleep(100);

            Console.WriteLine("Stopped consuming");
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_two_subscriptions_from_the_same_app_should_also_both_get_all_messages()
        {
            bus.Subscribe<MyMessage>(msg => Console.WriteLine(msg.Text));
            bus.Subscribe<MyMessage>(msg => Console.WriteLine(msg.Text));

            // allow time for messages to be consumed
            Thread.Sleep(100);

            Console.WriteLine("Stopped consuming");
        }
    }

    [Serializable]
    public class MyMessage
    {
        public string Text { get; set; }
    }
}

// ReSharper restore InconsistentNaming