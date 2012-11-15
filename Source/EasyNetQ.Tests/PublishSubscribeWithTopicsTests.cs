﻿// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class PublishSubscribeWithTopicsTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        [TearDown]
        public void TearDown()
        {
            if(bus != null) bus.Dispose();
        }

        private MyMessage CreateMessage()
        {
            return new MyMessage { Text = "Hello! " + Guid.NewGuid().ToString().Substring(0, 5) };
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Publish_some_messages_with_topics()
        {
            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Publish(CreateMessage(), x => x.WithTopic("X.A"));
                publishChannel.Publish(CreateMessage(), x => x.WithTopic("X.B"));
                publishChannel.Publish(CreateMessage(), x => x.WithTopic("Y.A"));
            }
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
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

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
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

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Publish_a_messages_without_a_topic()
        {
            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Publish(CreateMessage());
            }
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Subscribe_to_messages_without_a_topic_with_arguments()
        {
            var countdownEvent = new CountdownEvent(7);

            bus.Subscribe<MyMessage>("id1", msg =>
            {
                Console.WriteLine("I Get X: {0}", msg.Text);
                countdownEvent.Signal();
            }, x => x.WithArgument("x-made-up-argument", "any value"));

            countdownEvent.Wait(1000);
        }
    }
}

// ReSharper restore InconsistentNaming