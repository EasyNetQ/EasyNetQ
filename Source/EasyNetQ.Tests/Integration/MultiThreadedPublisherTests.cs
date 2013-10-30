// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture]
    public class MultiThreadedPublisherTests
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
            if (bus != null)
            {
                bus.Dispose();
            }
        }

        [Test, Explicit("Requires a local rabbitMq instance to run")]
        public void MultThreaded_publisher_should_not_cause_channel_proliferation()
        {
            var threads = new List<Thread>();

            for (int i = 0; i < 10; i++)
            {
                var thread = new Thread(x => bus.Publish(new MyMessage()));
                threads.Add(thread);
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        // First start the EasyNetQ.Tests.SimpleService console app.
        [Test, Explicit("Requires a local RabbitMQ instance to run")]
        public void MultiThreaded_requester_should_not_cause_channel_proliferation()
        {
            var threads = new List<Thread>();

            for (int i = 0; i < 10; i++)
            {
                var thread = new Thread(x =>
                {
                    bus.RequestAsync<TestRequestMessage, TestResponseMessage>(
                        new TestRequestMessage { Text = string.Format("Hello from client number: {0}! ", i) })
                        .ContinueWith(response => Console.WriteLine(response.Result.Text));
                });
                threads.Add(thread);
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
        }
    }
}

// ReSharper restore InconsistentNaming