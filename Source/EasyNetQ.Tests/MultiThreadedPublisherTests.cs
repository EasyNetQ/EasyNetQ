// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Tests
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

        // Failing test to demonstrate multi-threaded publisher creating a proliferation
        // of channels
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

            Assert.AreEqual(0, ((RabbitBus)bus).OpenChannelCount);
        }
    }
}

// ReSharper restore InconsistentNaming