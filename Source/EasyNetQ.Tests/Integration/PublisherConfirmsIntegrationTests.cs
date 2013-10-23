// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using EasyNetQ.Loggers;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture]
    [Explicit("Integration test, requires a RabbitMQ instance on localhost")]
    public class PublisherConfirmsIntegrationTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            var dlogger = new DelegateLogger
                {
                    InfoWriteDelegate = (s, o) => Console.WriteLine(s, o),
                    ErrorWriteDelegate = (s, o) => Console.WriteLine(s, o)
                };


            var logger = new ConsoleLogger();

            bus = RabbitHutch.CreateBus("host=localhost;publisherConfirms=true;timeout=10", 
                x => x.Register<IEasyNetQLogger>(_ => dlogger));
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test]
        public void Subscribe()
        {
            bus.Subscribe<MyMessage>("publish_confirms", message => {});
        }

        [Test]
        public void Should_be_able_to_interrupt_publishing()
        {
            // while we publish with publisher confirms on, we should be able to kill the 
            // RabbitMQ connection and see the publish successfully continue.

            while (true)
            {
                bus.Publish(new MyMessage
                    {
                        Text = "Hello World!"
                    });
            }
        }

        [Test]
        public void Should_be_able_to_publish_asynchronously()
        {
            var count = 0;
            while ((count++) < 10000)
            {
                bus.PublishAsync(new MyMessage
                    {
                        Text = string.Format("Message {0}", count)
                    }).ContinueWith(task =>
                        {
                            if (task.IsCompleted)
                            {
                                //Console.Out.WriteLine("{0} Completed", count);
                            }
                            if (task.IsFaulted)
                            {
                                Console.Out.WriteLine("\n\n");
                                Console.Out.WriteLine(task.Exception);
                                Console.Out.WriteLine("\n\n");
                            }
                        });
                //Thread.Sleep(1);
            }

            Thread.Sleep(10000);
        }
    }
}

// ReSharper restore InconsistentNaming