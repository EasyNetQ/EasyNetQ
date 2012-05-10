// ReSharper disable InconsistentNaming

using System;
using System.Net;
using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class SubscribeAsyncTests
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
            // DownloadStringTask comes from http://blogs.msdn.com/b/pfxteam/archive/2010/05/04/10007557.aspx

            bus.SubscribeAsync<MyMessage>("subscribe_async_test", message => 
                new WebClient().DownloadStringTask(new Uri("http://localhost:1338/?timeout=500"))
                    .ContinueWith(task => 
                        Console.WriteLine("Received: '{0}', Downloaded: '{1}'", 
                            message.Text, 
                            task.Result)));

            // give the test a chance to receive process the message
            Thread.Sleep(2000);
        }

        // 2. See above
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
            downloadTask.ContinueWith(task =>
                    Console.WriteLine("Downloaded: '{0}'",
                        task.Result));

            while (!downloadTask.IsCompleted)
            {
                Thread.Sleep(100);
            }
        }
    }
}

// ReSharper restore InconsistentNaming