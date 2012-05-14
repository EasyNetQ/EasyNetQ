// ReSharper disable InconsistentNaming

using EasyNetQ.Topology;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests.Topology
{
    [TestFixture]
    public class ExchangeTests
    {
        private IModel model;
        private ITopologyVisitor visitor;

        const string exchangeName = "speedster";

        [SetUp]
        public void SetUp()
        {
            model = MockRepository.GenerateStub<IModel>();
            visitor = new TopologyVisitor(model);
        }

        [Test]
        public void Should_create_a_direct_exchange()
        {
            var exchange = Exchange.CreateDirect(exchangeName);
            exchange.Visit(visitor);

            model.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "Direct", true));
        }

        [Test]
        public void Should_create_a_topic_exchange()
        {
            var exchange = Exchange.CreateTopic(exchangeName);
            exchange.Visit(visitor);

            model.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "Topic", true));
        }
    }
}

// ReSharper restore InconsistentNaming