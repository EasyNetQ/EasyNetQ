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
            bus = RabbitHutch.CreateBus("host=localhost");
            while(!bus.IsConnected) Thread.Sleep(10);
        }

        [TearDown]
        public void TearDown()
        {
            if(bus != null) bus.Dispose();
        }

        // 1. Run this first, should see no messages consumed
        // 3. Run this again (after publishing below), should see published messages appear
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_subscribe()
        {
            var autoResetEvent = new AutoResetEvent(false);
            bus.Subscribe<MyMessage>("test", message =>
            {
                Console.WriteLine(message.Text);
                autoResetEvent.Set();
            });

            // allow time for messages to be consumed
            autoResetEvent.WaitOne(1000);

            Console.WriteLine("Stopped consuming");
        }

        // 2. Run this a few times, should publish some messages
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_publish()
        {
            var message = new MyMessage { Text = "Hello! " + Guid.NewGuid().ToString().Substring(0, 5) };
            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Publish(message);
            }
            Console.Out.WriteLine("message.Text = {0}", message.Text);
        }

        // 4. Run this once to setup subscription, publish a few times using '2' above, run again to
        // see messages appear.
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_also_send_messages_to_second_subscriber()
        {
            var autoResetEvent = new AutoResetEvent(false);
            var messageQueue2 = RabbitHutch.CreateBus("host=localhost");
            messageQueue2.Subscribe<MyMessage>("test2", msg =>
            {
                Console.WriteLine(msg.Text);
                autoResetEvent.Set();
            });

            // allow time for messages to be consumed
            autoResetEvent.WaitOne(500);

            Console.WriteLine("Stopped consuming");
        }

        // 5. Run this once to setup subscriptions, publish a few times using '2' above, run again.
        // You should see two lots messages
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_two_subscriptions_from_the_same_app_should_also_both_get_all_messages()
        {
            var countdownEvent = new CountdownEvent(8);

            bus.Subscribe<MyMessage>("test_a", msg =>
            {
                Console.WriteLine(msg.Text);
                countdownEvent.Signal();
            });
            bus.Subscribe<MyOtherMessage>("test_b", msg =>
            {
                Console.WriteLine(msg.Text);
                countdownEvent.Signal();
            });
            bus.Subscribe<MyMessage>("test_c", msg =>
            {
                Console.WriteLine(msg.Text);
                countdownEvent.Signal();
            });
            bus.Subscribe<MyOtherMessage>("test_d", msg =>
            {
                Console.WriteLine(msg.Text);
                countdownEvent.Signal();
            });

            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Publish(new MyMessage { Text = "Hello! " + Guid.NewGuid().ToString().Substring(0, 5) });
                publishChannel.Publish(new MyMessage { Text = "Hello! " + Guid.NewGuid().ToString().Substring(0, 5) });

                publishChannel.Publish(new MyOtherMessage { Text = "Hello other! " + Guid.NewGuid().ToString().Substring(0, 5) });
                publishChannel.Publish(new MyOtherMessage { Text = "Hello other! " + Guid.NewGuid().ToString().Substring(0, 5) });
            }

            // allow time for messages to be consumed
            countdownEvent.Wait(1000);

            Console.WriteLine("Stopped consuming");
        }

        // 6. Run publish first using '2' above.
        // 7. Run this test, while it's running, restart the RabbitMQ service.
        // 
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Long_running_subscriber_should_survive_a_rabbit_restart()
        {
            var autoResetEvent = new AutoResetEvent(false);
            bus.Subscribe<MyMessage>("test", message =>
            {
                Console.Out.WriteLine("Restart RabbitMQ now.");
                new Timer(x =>
                {
                    Console.WriteLine(message.Text);
                    autoResetEvent.Set();
                }, null, 5000, Timeout.Infinite);
                
            });

            // allow time for messages to be consumed
            autoResetEvent.WaitOne(7000);

            Console.WriteLine("Stopped consuming");
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_subscribe_OK_before_connection_to_broker_is_complete()
        {
            var autoResetEvent = new AutoResetEvent(false);
            var testLocalBus = RabbitHutch.CreateBus("host=localhost");

            testLocalBus.Subscribe<MyMessage>("test", message =>
            {
                Console.Out.WriteLine("message.Text = {0}", message.Text);
                autoResetEvent.Set();
            });
            Console.WriteLine("--- subscribed ---");

            // allow time for bus to connect
            autoResetEvent.WaitOne(1000);
            testLocalBus.Dispose();
        }
    }

    public class MyMessage
    {
        public string Text { get; set; }
    }

    public class MyOtherMessage
    {
        public string Text { get; set; }
    }
}

// ReSharper restore InconsistentNaming