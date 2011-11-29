// ReSharper disable InconsistentNaming

using NUnit.Framework;
using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class CorrelationIdTests
    {
        private const string correlationId = "the correlation id";

        private IBus bus;
        private MockModel model;

        [SetUp]
        public void SetUp()
        {
            model = new MockModel();
            bus = new TestBusFactory
            {
                Model = model,
                GetCorrelationId = () => correlationId
            }.CreateBusWithMockAmqpClient();
        }

        [Test]
        public void Should_write_correlation_id_to_properties()
        {
            IBasicProperties basicProperties = null;

            model.BasicPublishAction = (a, b, properties, body) =>
            {
                basicProperties = properties;
            };

            bus.Publish(new TestMessage());

            basicProperties.ShouldNotBeNull();
            basicProperties.CorrelationId.ShouldNotBeNull();
            basicProperties.CorrelationId.ShouldEqual(correlationId);
        }
    }
}

// ReSharper restore InconsistentNaming