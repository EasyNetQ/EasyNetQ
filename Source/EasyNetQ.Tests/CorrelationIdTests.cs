// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.Tests.Mocking;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class CorrelationIdTests
    {
        private const string correlationId = "the correlation id";

        private IBus bus;
        private MockBuilder mockBuilder;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder(x => x.Register<Func<string>>(_ => () => correlationId));
            bus = mockBuilder.Bus;
        }

        [Test]
        public void Should_write_correlation_id_to_properties()
        {
            IBasicProperties basicProperties = null;

            mockBuilder.Channel.Stub(x => x.BasicPublish(
                Arg<string>.Is.Anything,
                Arg<string>.Is.Anything,
                Arg<IBasicProperties>.Is.Anything,
                Arg<byte[]>.Is.Anything)).Callback<string, string, IBasicProperties, byte[]>(
                    (a, b, properties, body) =>
                        {
                            basicProperties = properties;
                            return true;
                        });

            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Publish(new TestMessage());
            }

            basicProperties.ShouldNotBeNull();
            basicProperties.CorrelationId.ShouldNotBeNull();
            basicProperties.CorrelationId.ShouldEqual(correlationId);
        }
    }
}

// ReSharper restore InconsistentNaming