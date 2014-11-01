// ReSharper disable InconsistentNaming

using System;
using System.Text;
using System.Threading;
using EasyNetQ.Loggers;
using EasyNetQ.Topology;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
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


        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_subscribe_as_exlusive()
        {
            var countdownEvent = new CountdownEvent(10);
            var firstCount = 0;
            var secondCount = 0;

            bus.Subscribe<MyMessage>("test", message =>
                {
                    countdownEvent.Signal();
                    Interlocked.Increment(ref firstCount);
                    Console.WriteLine("[1] " + message.Text);
                }, x => x.AsExclusive());
            bus.Subscribe<MyMessage>("test", message =>
                {
                    countdownEvent.Signal();
                    Interlocked.Increment(ref secondCount);
                    Console.WriteLine("[2] " + message.Text);
                }, x => x.AsExclusive());

            for (var i = 0; i < 10; ++i)
                bus.Publish(new MyMessage
                    {
                        Text = "Exclusive " + i
                    });
            countdownEvent.Wait(10 * 1000);
            Assert.IsTrue(firstCount == 10 && secondCount == 0 || firstCount == 0 && secondCount == 10);
            Console.WriteLine("Stopped consuming");
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Long_running_exclusive_subscriber_should_survive_a_rabbit_restart()
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
            }, x => x.AsExclusive());

            // allow time for messages to be consumed
            autoResetEvent.WaitOne(15000);

            Console.WriteLine("Stopped consuming");
        }


        // 2. Run this a few times, should publish some messages
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_publish()
        {
            var message = new MyMessage { Text = "Hello! " + Guid.NewGuid().ToString().Substring(0, 5) };
            bus.Publish(message);
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

            bus.Publish(new MyMessage { Text = "Hello! " + Guid.NewGuid().ToString().Substring(0, 5) });
            bus.Publish(new MyMessage { Text = "Hello! " + Guid.NewGuid().ToString().Substring(0, 5) });

            bus.Publish(new MyOtherMessage { Text = "Hello other! " + Guid.NewGuid().ToString().Substring(0, 5) });
            bus.Publish(new MyOtherMessage { Text = "Hello other! " + Guid.NewGuid().ToString().Substring(0, 5) });

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

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_round_robin_between_subscribers()
        {
            Action<IServiceRegister> setNoDebugLogger = register =>
                register.Register<IEasyNetQLogger>(_ => new DelegateLogger());

            const string connectionString = "host=localhost;prefetchcount=100";

            var publishBus = RabbitHutch.CreateBus(connectionString, setNoDebugLogger);
            var subscribeBus1 = RabbitHutch.CreateBus(connectionString, setNoDebugLogger);
            var subscribeBus2 = RabbitHutch.CreateBus(connectionString, setNoDebugLogger);

            // first set up the subscribers
            subscribeBus1.Subscribe<MyMessage>("roundRobinTest", message => 
                Console.WriteLine("Subscriber 1: {0}", message.Text));
            subscribeBus2.Subscribe<MyMessage>("roundRobinTest", message => 
                Console.WriteLine("Subscriber 2: {0}", message.Text));

            // now publish some messages
            for (int i = 0; i < 50; i++)
            {
                publishBus.Publish(new MyMessage { Text = string.Format("Message{0}", i) });
            }

            Thread.Sleep(1000);

            publishBus.Dispose();
            subscribeBus1.Dispose();
            subscribeBus2.Dispose();
        }

        public static void SetImmediateToTrue()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");

            var properties = new MessageProperties();
            var body = Encoding.UTF8.GetBytes("Test");

            var message =
            new Message<MyMessage>(new MyMessage()
            {
                Text = "Hello"
            });

            bus.Advanced.Publish(Exchange.GetDefault(), "test_queue", true, true, message);

            //bus.Advanced.Publish(Exchange.GetDefault(), "test_queue", true, true, properties, body);

            bus.Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming