// ReSharper disable InconsistentNaming

using EasyNetQ.Topology;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests.Topology
{
    [TestFixture]
    public class TopologyTests
    {
        private IModel model;
        private ITopologyVisitor visitor;

        private const string exchangeName = "speedster";
        private const string queueName = "roadster";
        private const string routingKey = "drop_head";

        [SetUp]
        public void SetUp()
        {
            model = MockRepository.GenerateStub<IModel>();
            visitor = new TopologyBuilder(model);
        }

        //  XD
        [Test]
        public void Should_create_a_direct_exchange()
        {
            var exchange = Exchange.DeclareDirect(exchangeName);
            exchange.Visit(visitor);

            model.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "Direct", true));
        }

        //  XT
        [Test]
        public void Should_create_a_topic_exchange()
        {
            var exchange = Exchange.DeclareTopic(exchangeName);
            exchange.Visit(visitor);

            model.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "Topic", true));
        }

        // XF
        [Test]
        public void Should_create_a_fanout_exchange()
        {
            var exchange = Exchange.DeclareFanout(exchangeName);
            exchange.Visit(visitor);

            model.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "Fanout", true));
        }

        // XD (default)
        [Test]
        public void Should_get_the_default_exchange()
        {
            var exchange = Exchange.GetDefault();
            exchange.Visit(visitor);

            model.AssertWasCalled(x => x.ExchangeDeclare("", "Direct", true));
        }

        // QD
        [Test]
        public void Should_create_a_durable_queue()
        {
            var queue = Queue.DeclareDurable(queueName);
            queue.Visit(visitor);

            model.AssertWasCalled(x => x.QueueDeclare(queueName, true, false, false, null));
        }

        // QT
        [Test]
        public void Should_create_a_transiet_queue()
        {
            var queue = Queue.DeclareTransient(queueName);
            queue.Visit(visitor);

            model.AssertWasCalled(x => x.QueueDeclare(queueName, false, true, true, null));
        }

        // QT (unnamed)
        [Test]
        public void Should_create_an_unnamed_transient_queue()
        {
            const string rabbit_generated_queue_name = "rabbit_generated_queue_name";
            model.Stub(x => x.QueueDeclare()).Return(new QueueDeclareOk(rabbit_generated_queue_name, 0, 0));

            var queue = Queue.DeclareTransient();
            queue.Visit(visitor);

            model.AssertWasCalled(x => x.QueueDeclare());

            queue.Name.ShouldEqual(rabbit_generated_queue_name);
        }

        //  XD -> QD
        [Test]
        public void Should_be_able_to_bind_a_queue_to_an_exchange()
        {
            var queue = Queue.DeclareDurable(queueName);
            var exchange = Exchange.DeclareDirect(exchangeName);

            queue.BindTo(exchange, routingKey);
            queue.Visit(visitor);

            model.AssertWasCalled(x => x.QueueDeclare(queueName, true, false, false, null));
            model.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "Direct", true));
            model.AssertWasCalled(x => x.QueueBind(queueName, exchangeName, routingKey));
        }

        // XD   ->  QD (unnamed)
        [Test]
        public void Should_be_able_to_bind_an_unnamed_queue_to_an_exchange()
        {
            const string rabbit_generated_queue_name = "rabbit_generated_queue_name";
            model.Stub(x => x.QueueDeclare()).Return(new QueueDeclareOk(rabbit_generated_queue_name, 0, 0));

            var queue = Queue.DeclareTransient();
            var exchange = Exchange.DeclareDirect(exchangeName);

            queue.BindTo(exchange, routingKey);
            queue.Visit(visitor);

            model.AssertWasCalled(x => x.QueueDeclare());
            model.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "Direct", true));
            model.AssertWasCalled(x => x.QueueBind(rabbit_generated_queue_name, exchangeName, routingKey));
        }

        //      ->
        //  XD  ->  QD
        //      ->
        [Test]
        public void Should_be_able_to_have_multiple_bindings_to_an_exchange()
        {
            var queue = Queue.DeclareDurable(queueName);
            var exchange = Exchange.DeclareDirect(exchangeName);

            queue.BindTo(exchange, "a", "b", "c");
            queue.Visit(visitor);

            model.AssertWasCalled(x => x.QueueDeclare(queueName, true, false, false, null));
            model.AssertWasCalled(x => x.ExchangeDeclare(exchangeName, "Direct", true));
            model.AssertWasCalled(x => x.QueueBind(queueName, exchangeName, "a"));
            model.AssertWasCalled(x => x.QueueBind(queueName, exchangeName, "b"));
            model.AssertWasCalled(x => x.QueueBind(queueName, exchangeName, "c"));
        }

        //  XD  ->  XD
        [Test]
        public void Should_be_able_to_bind_an_exchange_to_an_exchange()
        {
            var sourceExchange = Exchange.DeclareDirect("source");
            var destinationExchange = Exchange.DeclareDirect("destination");

            destinationExchange.BindTo(sourceExchange, routingKey);
            destinationExchange.Visit(visitor);

            model.AssertWasCalled(x => x.ExchangeDeclare("destination", "Direct", true));
            model.AssertWasCalled(x => x.ExchangeDeclare("source", "Direct", true));
            model.AssertWasCalled(x => x.ExchangeBind("destination", "source", routingKey));
        }

        // XD -> XD -> QD
        [Test]
        public void Should_be_able_to_bind_a_queue_to_an_exchange_and_then_to_an_exchange()
        {
            var sourceExchange = Exchange.DeclareDirect("source");
            var destinationExchange = Exchange.DeclareDirect("destination");
            var queue = Queue.DeclareDurable(queueName);

            destinationExchange.BindTo(sourceExchange, routingKey);
            queue.BindTo(destinationExchange, routingKey);

            queue.Visit(visitor);

            model.AssertWasCalled(x => x.QueueDeclare(queueName, true, false, false, null));
            model.AssertWasCalled(x => x.ExchangeDeclare("destination", "Direct", true));
            model.AssertWasCalled(x => x.QueueBind(queueName, "destination", routingKey));
            model.AssertWasCalled(x => x.ExchangeDeclare("source", "Direct", true));
            model.AssertWasCalled(x => x.ExchangeBind("destination", "source", routingKey));
        }
    }
}

// ReSharper restore InconsistentNaming