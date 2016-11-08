// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using NSubstitute;

namespace EasyNetQ.Tests.ProducerTests
{
    [TestFixture]
    public class When_IModel_throws_because_of_closed_connection
    {
        private MockBuilder mockBuilder;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder("host=localhost;timeout=1");

            mockBuilder.NextModel
                .WhenForAnyArgs(x => x.ExchangeDeclare(null, null, false, false, null))
                .Do(x =>
                    {
                        var args = new ShutdownEventArgs(ShutdownInitiator.Peer, 320, 
                            "CONNECTION_FORCED - Closed via management plugin");
                        throw new OperationInterruptedException(args);
                    });
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_try_to_reconnect_until_timeout()
        {
            try
            {
                mockBuilder.Bus.Publish(new MyMessage { Text = "Hello World" });
            }
            catch (AggregateException aggregateException)
            {
                if (!(aggregateException.InnerException is TimeoutException))
                {
                    throw;
                }
            }
        }
    }
}

// ReSharper restore InconsistentNaming