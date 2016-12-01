// ReSharper disable InconsistentNaming

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    public class SubscribeAsyncTests : IDisposable
    {
        private IBus bus;

        public SubscribeAsyncTests()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        // 1. Start LongRunningServer.js (a little node.js webserver in this directory)
        // 2. Run this test to setup the subscription
        // 3. Publish a message by running Publish_a_test_message_for_subscribe_async below
        // 4. Run this test again to see the message consumed.
        // You should see all 10 messages get processes at once, even though each web request
        // takes 150 ms.
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_subscribe_async()
        {
            var countdownEvent = new CountdownEvent(10);
            // DownloadStringTask comes from http://blogs.msdn.com/b/pfxteam/archive/2010/05/04/10007557.aspx

            bus.SubscribeAsync<MyMessage>("subscribe_async_test", message => 
                new HttpClient().GetStringAsync(new Uri("http://localhost:1338/?timeout=500"))
                    .ContinueWith(task =>
                    {
                        Console.WriteLine("Received: '{0}', Downloaded: '{1}'",
                            message.Text,
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
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_handle_multiple_async_IO_operations_in_a_handler()
        {
            bus.SubscribeAsync<MyMessage>("subscribe_async_test_2", message =>
            {
                var downloadTasks = new[]
                {
                    new HttpClient().GetStringAsync(new Uri("http://localhost:1338/?timeout=500")),
                    new HttpClient().GetStringAsync(new Uri("http://localhost:1338/?timeout=501")),
                    new HttpClient().GetStringAsync(new Uri("http://localhost:1338/?timeout=502")),
                    new HttpClient().GetStringAsync(new Uri("http://localhost:1338/?timeout=503")),
                    new HttpClient().GetStringAsync(new Uri("http://localhost:1338/?timeout=504")),
                };

                return Task.Factory.ContinueWhenAll(downloadTasks, tasks =>
                {
                    Console.WriteLine("Finished processing: {0}", message.Text);
                    foreach (var task in tasks)
                    {
                        Console.WriteLine("\tDownloaded: {0}", task.Result);
                    }
                });
            });

            // give the test a chance to receive process the message
            Thread.Sleep(2000);
        }

        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Should_be_able_to_handle_async_tasks_in_sequence()
        {
            bus.SubscribeAsync<MyMessage>("subscribe_async_test_2", message =>
            {
                Console.WriteLine("Got message: {0}", message.Text);
                var firstRequestTask = new HttpClient().GetStringAsync(new Uri("http://localhost:1338/?timeout=100"));

                return firstRequestTask.ContinueWith(task1 =>
                {
                    Console.WriteLine("First Response for: {0}, => {1}", message.Text, task1.Result);
                    var secondRequestTask = new HttpClient()
                        .GetStringAsync(new Uri("http://localhost:1338/?timeout=501"));

                    return secondRequestTask.ContinueWith(task2 => 
                        Console.WriteLine("Second Response for: {0}, => {1}", message.Text, task2.Result));
                });
            });

            // give the test a chance to receive process the message
            Thread.Sleep(2000);
        }

        // See above
        [Fact][Explicit("Needs a Rabbit instance on localhost to work")]
        public void Publish_a_test_message_for_subscribe_async()
        {
            for (var i = 0; i < 10; i++)
            {
                bus.Publish(new MyMessage { Text = "Hi from the publisher " + i });
            }
        }

        // make sure LongRunningServer.js is working by using this...
        public void WebClientSpike()
        {
            var downloadTask = new HttpClient().GetStringAsync(new Uri("http://localhost:1338/?timeout=150"));
            Console.WriteLine("Downloaded: '{0}'", downloadTask.Result);
        }
    }
}

// ReSharper restore InconsistentNaming