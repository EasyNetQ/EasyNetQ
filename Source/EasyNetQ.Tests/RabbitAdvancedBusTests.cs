﻿// ReSharper disable InconsistentNaming

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class RabbitAdvancedBusTests
    {
        private IAdvancedBus advancedBus;

        [SetUp]
        public void SetUp()
        {
            advancedBus = RabbitHutch.CreateBus("host=localhost").Advanced;
        }

        [TearDown]
        public void TearDown()
        {
            if(advancedBus != null) advancedBus.Dispose();
        }

        [Test, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_be_able_to_do_a_simple_publish_and_subscribe()
        {
            const string routingKey = "advanced_test_routing_key";

            var autoResetEvent = new AutoResetEvent(false);

            var exchange = Exchange.DeclareDirect("advanced_test_exchange");
            var queue = Queue.DeclareDurable("advanced_test_queue");
            queue.BindTo(exchange, routingKey);

            advancedBus.Subscribe<MyMessage>(queue, (msg, messageReceivedInfo) => 
                Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("Got Message: {0}", msg.GetBody().Text);
                    Console.WriteLine("ConsumerTag: {0}", messageReceivedInfo.ConsumerTag);
                    Console.WriteLine("DeliverTag: {0}", messageReceivedInfo.DeliverTag);
                    Console.WriteLine("Redelivered: {0}", messageReceivedInfo.Redelivered);
                    Console.WriteLine("Exchange: {0}", messageReceivedInfo.Exchange);
                    Console.WriteLine("RoutingKey: {0}", messageReceivedInfo.RoutingKey);
                    autoResetEvent.Set();
                }));

            var myMessage = new MyMessage {Text = "Hello from the publisher"};
            var message = new Message<MyMessage>(myMessage);

            message.Properties.AppId = "my_app_id";
            message.Properties.ReplyTo = "my_reply_queue";

            using (var channel = advancedBus.OpenPublishChannel())
            {
                channel.Publish(exchange, routingKey, message);
            }

            // give the test time to complete
            autoResetEvent.WaitOne(1000);
        }

        [Test, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_be_able_to_do_publish_subscribe_via_default_exchange()
        {
            var autoResetEvent = new AutoResetEvent(false);

            var queue = Queue.DeclareTransient();

            advancedBus.Subscribe<MyMessage>(queue, (message, messageRecievedInfo) => 
                Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("Got message: {0}", message.GetBody().Text);
                    autoResetEvent.Set();
                }));

            using (var channel = advancedBus.OpenPublishChannel())
            {
                channel.Publish(Exchange.GetDefault(), queue.Name, 
                    new Message<MyMessage>(new MyMessage { Text = "Hello from the publisher"}));
            }

            // give the test time to complete
            autoResetEvent.WaitOne(500);
        }

        [Test, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_be_able_to_publish_the_wrong_message_type_to_a_subscriber()
        {
            var queue = Queue.DeclareTransient();

            advancedBus.Subscribe<MyMessage>(queue, (message, messageRecievedInfo) => 
                Task.Factory.StartNew(() => { }));

            using (var channel = advancedBus.OpenPublishChannel())
            {
                channel.Publish(Exchange.GetDefault(), queue.Name,
                    new Message<MyOtherMessage>(new MyOtherMessage { Text = "Hello from the publisher" }));
            }

            // give the test time to complete
            Thread.Sleep(1000);
        }

        [Test, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_be_able_to_pass_reply_to_address_to_consumer()
        {
            var autoResetEvent = new AutoResetEvent(false);

            var queue = Queue.DeclareTransient();

            advancedBus.Subscribe<MyMessage>(queue, (message, messageRecievedInfo) => 
                Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("Got reply to address: {0}", message.Properties.ReplyTo);
                    autoResetEvent.Set();
                }));

            var messageToPublish = new Message<MyMessage>(new MyMessage {Text = "Hello from the publisher"});
            messageToPublish.Properties.ReplyTo = "the_reply_to_address";

            using (var channel = advancedBus.OpenPublishChannel())
            {
                channel.Publish(Exchange.GetDefault(), queue.Name, messageToPublish);
            }

            // give the test time to complete
            autoResetEvent.WaitOne(1000);
        }

        [Test, Explicit("Requires a RabbitMQ instance on localhost")]
        public void Should_be_able_to_bind_a_chain_of_exchanges()
        {
            var autoResetEvent = new AutoResetEvent(false);

            var exchange1 = Exchange.DeclareDirect("advanced_test_exchange_1");
            var exchange2 = Exchange.DeclareDirect("advanced_test_exchange_2");
            var exchange3 = Exchange.DeclareDirect("advanced_test_exchange_3");
            var queue = Queue.DeclareDurable("advanced_test_queue");

            queue.BindTo(exchange3, "route1");
            exchange3.BindTo(exchange2, "route1");
            exchange2.BindTo(exchange1, "route1");

            advancedBus.Subscribe<MyMessage>(queue, (message, messageRecievedInfo) =>
                Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("Got Message: {0}", message.GetBody().Text);
                    autoResetEvent.Set();
                }));

            using (var channel = advancedBus.OpenPublishChannel())
            {
                channel.Publish(exchange1, "route1",
                    new Message<MyMessage>(new MyMessage { Text = "Hello from the publisher" }));
            }

            // give the test time to complete
            autoResetEvent.WaitOne(1000);
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_do_topic_based_routing()
        {
            var countdownEvent = new CountdownEvent(4);

            var exchange = Exchange.DeclareTopic("advanced_test_topic_exchange");
            var queue1 = Queue.DeclareDurable("advanced_test_queue_1");
            var queue2 = Queue.DeclareDurable("advanced_test_queue_2");
            var queue3 = Queue.DeclareDurable("advanced_test_queue_3");

            queue1.BindTo(exchange, "A.*");
            queue2.BindTo(exchange, "B.X");
            queue3.BindTo(exchange, "*.Y");

            advancedBus.Subscribe<MyMessage>(queue1, (message, messageRecievedInfo) =>
                Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("1 Got Message: {0}", messageRecievedInfo.RoutingKey);
                    countdownEvent.Signal();
                }));
            advancedBus.Subscribe<MyMessage>(queue2, (message, messageRecievedInfo) =>
                Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("2 Got Message: {0}", messageRecievedInfo.RoutingKey);
                    countdownEvent.Signal();
                }));
            advancedBus.Subscribe<MyMessage>(queue3, (message, messageRecievedInfo) =>
                Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("3 Got Message: {0}", messageRecievedInfo.RoutingKey);
                    countdownEvent.Signal();
                }));

            using (var channel = advancedBus.OpenPublishChannel())
            {
                channel.Publish(exchange, "A.Y", new Message<MyMessage>(new MyMessage()));
                channel.Publish(exchange, "B.X", new Message<MyMessage>(new MyMessage()));
                channel.Publish(exchange, "B.Y", new Message<MyMessage>(new MyMessage()));
            }

            // give the test time to complete
            countdownEvent.Wait(1000);
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_publish_subscribe_raw_bytes()
        {
            var autoResetEvent = new AutoResetEvent(false);

            var queue = Queue.DeclareTransient();

            advancedBus.Subscribe(queue, (message, properties, messageRecievedInfo) =>
                Task.Factory.StartNew(() =>
                {
                    var messageString = Encoding.UTF8.GetString(message);
                    Console.WriteLine("Got message: '{0}'", messageString);
                    autoResetEvent.Set();
                }));

            using (var channel = advancedBus.OpenPublishChannel())
            {
                channel.Publish(Exchange.GetDefault(), queue.Name, new MessageProperties(), 
                    Encoding.UTF8.GetBytes("Hello World"));
            }

            // give the test time to complete
            autoResetEvent.WaitOne(1000);
        }
    }
}

// ReSharper restore InconsistentNaming