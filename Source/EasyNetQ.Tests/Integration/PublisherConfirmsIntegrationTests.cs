// ReSharper disable InconsistentNaming

using System;
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
    }
}

// ReSharper restore InconsistentNaming