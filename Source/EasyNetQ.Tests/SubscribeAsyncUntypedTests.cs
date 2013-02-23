﻿// ReSharper disable InconsistentNaming

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class SubscribeAsyncUntypedTests
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
            bus.Dispose();
        }

        // 1. Start LongRunningServer.js (a little node.js webserver in this directory)
        // 2. Run this test to setup the subscription
        // 3. Publish a message by running Publish_a_test_message_for_subscribe_async below
        // 4. Run this test again to see the message consumed.
        // You should see all 10 messages get processes at once, even though each web request
        // takes 150 ms.
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_subscribe_async()
        {
            var countdownEvent = new CountdownEvent(10);
            // DownloadStringTask comes from http://blogs.msdn.com/b/pfxteam/archive/2010/05/04/10007557.aspx

            bus.SubscribeAsync("subscribe_async_test", typeof(MyMessage), message => 
                new WebClient().DownloadStringTask(new Uri("http://localhost:1338/?timeout=500"))
                    .ContinueWith(task =>
                    {
                        Console.WriteLine("Received: '{0}', Downloaded: '{1}'",
                            ((MyMessage)message).Text,
                            task.Result);
                        countdownEvent.Signal();
                    }));

            // give the test a chance to receive process the message
            countdownEvent.Wait(2000);
        }

        // 1. Start LongRunningServer.js (a little node.js webserver in this directory)
        // 2. Run this test to setup the subscription
        // 3. Publish a message by running Publish_a_test_message_for_subscribe_async below
        // 4. Run this test again to see the message consumed.
        // You should see all 10 messages get processes at once, even though each web request
        // takes 150 ms.
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_handle_multiple_async_IO_operations_in_a_handler()
        {
            bus.SubscribeAsync("subscribe_async_test_2", typeof(MyMessage), message =>
            {
                var downloadTasks = new[]
                {
                    new WebClient().DownloadStringTask(new Uri("http://localhost:1338/?timeout=500")),
                    new WebClient().DownloadStringTask(new Uri("http://localhost:1338/?timeout=501")),
                    new WebClient().DownloadStringTask(new Uri("http://localhost:1338/?timeout=502")),
                    new WebClient().DownloadStringTask(new Uri("http://localhost:1338/?timeout=503")),
                    new WebClient().DownloadStringTask(new Uri("http://localhost:1338/?timeout=504")),
                };

                return Task.Factory.ContinueWhenAll(downloadTasks, tasks =>
                {
                    Console.WriteLine("Finished processing: {0}", ((MyMessage)message).Text);
                    foreach (var task in tasks)
                    {
                        Console.WriteLine("\tDownloaded: {0}", task.Result);
                    }
                });
            });

            // give the test a chance to receive process the message
            Thread.Sleep(2000);
        }

        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_handle_async_tasks_in_sequence()
        {
            bus.SubscribeAsync("subscribe_async_test_2", typeof(MyMessage), msg =>
            {
                var message = (MyMessage)msg;
                Console.WriteLine("Got message: {0}", message.Text);
                var firstRequestTask = new WebClient().DownloadStringTask(new Uri("http://localhost:1338/?timeout=100"));

                return firstRequestTask.ContinueWith(task1 =>
                {
                    Console.WriteLine("First Response for: {0}, => {1}", message.Text, task1.Result);
                    var secondRequestTask = new WebClient()
                        .DownloadStringTask(new Uri("http://localhost:1338/?timeout=501"));

                    return secondRequestTask.ContinueWith(task2 => 
                        Console.WriteLine("Second Response for: {0}, => {1}", message.Text, task2.Result));
                });
            });

            // give the test a chance to receive process the message
            Thread.Sleep(2000);
        }

        // See above
        [Test, Explicit("Needs a Rabbit instance on localhost to work")]
        public void Publish_a_test_message_for_subscribe_async()
        {
            using (var channel = bus.OpenPublishChannel())
            {
                for (var i = 0; i < 10; i++)
                {
                    channel.Publish(new MyMessage { Text = "Hi from the publisher " + i });
                }
            }
        }

        // make sure LongRunningServer.js is working by using this...
        public void WebClientSpike()
        {
            var downloadTask = new WebClient().DownloadStringTask(new Uri("http://localhost:1338/?timeout=150"));
            Console.WriteLine("Downloaded: '{0}'", downloadTask.Result);
        }
    }
}

// ReSharper restore InconsistentNaming