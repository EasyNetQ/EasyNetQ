using System.Collections.Generic;
// ReSharper disable InconsistentNaming
using System.Collections;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using NUnit.Framework;
using NSubstitute;
using System.Linq;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class When_a_queue_is_declared
    {
        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;
        private IQueue queue;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();

            advancedBus = mockBuilder.Bus.Advanced;
            queue = advancedBus.QueueDeclare(
                "my_queue", 
                passive: false, 
                durable: false, 
                exclusive: true,
                autoDelete: true,
                perQueueMessageTtl: 1000,
                expires: 2000,
                maxPriority: 10);
        }


        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_return_a_queue()
        {
            queue.ShouldNotBeNull();
            queue.Name.ShouldEqual("my_queue");
        }

        [Test]
        public void Should_declare_the_queue()
        {
            mockBuilder.Channels[0].Received().QueueDeclare(
                   Arg.Is("my_queue"),
                   Arg.Is(false),
                   Arg.Is(true),
                   Arg.Is(true),
                   Arg.Is<IDictionary<string, object>>(args =>
                       ((int)args["x-message-ttl"] == 1000) &&
                       ((int)args["x-expires"] == 2000) &&
                       ((int)args["x-max-priority"] == 10)));
        }
    }

    [TestFixture]
    public class When_a_queue_is_declared_With_NonEmptyDeadLetterExchange
    {
        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;
        private IQueue queue;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();

            advancedBus = mockBuilder.Bus.Advanced;
            queue = advancedBus.QueueDeclare(
                "my_queue",
                passive: false,
                durable: false,
                exclusive: true,
                autoDelete: true,
                perQueueMessageTtl: 1000,
                expires: 2000,
                maxPriority: 10,
                deadLetterExchange: "my_exchange",
                deadLetterRoutingKey: "my_routing_key");
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_return_a_queue()
        {
            queue.ShouldNotBeNull();
            queue.Name.ShouldEqual("my_queue");
        }

        [Test]
        public void Should_declare_the_queue()
        {
            mockBuilder.Channels[0].Received().QueueDeclare(
                  Arg.Is("my_queue"),
                  Arg.Is(false),
                  Arg.Is(true),
                  Arg.Is(true),
                  Arg.Is<IDictionary<string, object>>(args =>
                      ((int)args["x-message-ttl"] == 1000) &&
                      ((int)args["x-expires"] == 2000) &&
                      ((int)args["x-max-priority"] == 10) &&
                      ((string)args["x-dead-letter-exchange"] == "my_exchange") &&
                      ((string)args["x-dead-letter-routing-key"] == "my_routing_key")));
        }
    }

    [TestFixture]
    public class When_a_queue_is_declared_With_EmptyDeadLetterExchange
    {
        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;
        private IQueue queue;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();

            advancedBus = mockBuilder.Bus.Advanced;
            queue = advancedBus.QueueDeclare(
                "my_queue",
                passive: false,
                durable: false,
                exclusive: true,
                autoDelete: true,
                perQueueMessageTtl: 1000,
                expires: 2000,
                maxPriority: 10,
                deadLetterExchange: "",
                deadLetterRoutingKey: "my_queue2");
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_return_a_queue()
        {
            queue.ShouldNotBeNull();
            queue.Name.ShouldEqual("my_queue");
        }

        [Test]
        public void Should_declare_the_queue()
        {
            mockBuilder.Channels[0].Received().QueueDeclare(
                 Arg.Is("my_queue"),
                 Arg.Is(false),
                 Arg.Is(true),
                 Arg.Is(true),
                 Arg.Is<IDictionary<string, object>>(args =>
                     ((int)args["x-message-ttl"] == 1000) &&
                     ((int)args["x-expires"] == 2000) &&
                     ((int)args["x-max-priority"] == 10) &&
                     ((string)args["x-dead-letter-exchange"] == "") &&
                     ((string)args["x-dead-letter-routing-key"] == "my_queue2")));
        }
    }

    [TestFixture]
    public class When_a_queue_is_deleted
    {
        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();
            advancedBus = mockBuilder.Bus.Advanced;

            var queue = new Topology.Queue("my_queue", false);
            advancedBus.QueueDelete(queue);
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_delete_the_queue()
        {
            mockBuilder.Channels[0].Received().QueueDelete("my_queue", false, false);
        }
    }

    [TestFixture]
    public class When_an_exchange_is_declared
    {
        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;
        private IExchange exchange;
        private IDictionary arguments;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();
            advancedBus = mockBuilder.Bus.Advanced;

            mockBuilder.NextModel.WhenForAnyArgs(x => x.ExchangeDeclare(null, null, false, false, null))
                .Do(x =>
                {
                    arguments = x[4] as IDictionary;
                });

            exchange = advancedBus.ExchangeDeclare(
                "my_exchange", 
                ExchangeType.Direct, 
                false, 
                false, 
                true, 
                true, 
                "my.alternate.exchange");
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_return_an_exchange_instance()
        {
            exchange.ShouldNotBeNull();
            exchange.Name.ShouldEqual("my_exchange");
        }

        [Test]
        public void Should_declare_an_exchange()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare(
                    Arg.Is("my_exchange"),
                    Arg.Is("direct"),
                    Arg.Is(false),
                    Arg.Is(true),
                    Arg.Any<IDictionary<string, object>>());
        }

        [Test]
        public void Should_add_correct_arguments()
        {
            arguments.ShouldNotBeNull();
            arguments["alternate-exchange"].ShouldEqual("my.alternate.exchange");
        }
    }

    [TestFixture]
    public class When_an_exchange_is_declared_passively
    {
        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;
        private IExchange exchange;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();
            advancedBus = mockBuilder.Bus.Advanced;

            exchange = advancedBus.ExchangeDeclare("my_exchange", ExchangeType.Direct, passive: true);
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_return_an_exchange_instance()
        {
            exchange.ShouldNotBeNull();
            exchange.Name.ShouldEqual("my_exchange");
        }

        [Test]
        public void Should_passively_declare_exchange()
        {
            mockBuilder.Channels.Count.ShouldEqual(1);
            mockBuilder.Channels[0].Received().ExchangeDeclarePassive(Arg.Is("my_exchange"));
        }
    }

    [TestFixture]
    public class When_an_exchange_is_deleted
    {
        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();
            advancedBus = mockBuilder.Bus.Advanced;

            var exchange = new Exchange("my_exchange");
            advancedBus.ExchangeDelete(exchange);
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_delete_the_queue()
        {
            mockBuilder.Channels[0].Received().ExchangeDelete("my_exchange", false);
        }
    }

    [TestFixture]
    public class When_a_queue_is_bound_to_an_exchange
    {
        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;
        private IBinding binding;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();
            advancedBus = mockBuilder.Bus.Advanced;

            var exchange = new Exchange("my_exchange");
            var queue = new Topology.Queue("my_queue", false);

            binding = advancedBus.Bind(exchange, queue, "my_routing_key");
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_create_a_binding_instance()
        {
            binding.ShouldNotBeNull();
            binding.RoutingKey.ShouldEqual("my_routing_key");
            binding.Exchange.Name.ShouldEqual("my_exchange");
            binding.Bindable.ShouldBe<IQueue>();
            ((IQueue) binding.Bindable).Name.ShouldEqual("my_queue");
        }

        [Test]
        public void Should_declare_a_binding()
        {
            mockBuilder.Channels[0].Received().QueueBind(
                Arg.Is("my_queue"),
                Arg.Is("my_exchange"),
                Arg.Is("my_routing_key"),
                Arg.Is<Dictionary<string, object>>(x => x.SequenceEqual(new Dictionary<string, object>())));
        }
    }

    [TestFixture]
    public class When_a_queue_is_unbound_from_an_exchange
    {
        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;
        private IBinding binding;

        [SetUp]
        public void SetUp()
        {
            mockBuilder = new MockBuilder();
            advancedBus = mockBuilder.Bus.Advanced;

            var exchange = new Exchange("my_exchange");
            var queue = new Topology.Queue("my_queue", false);
            binding = advancedBus.Bind(exchange, queue, "my_routing_key");
            advancedBus.BindingDelete(binding);
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Test]
        public void Should_unbind_the_exchange()
        {
            mockBuilder.Channels[0].Received().QueueUnbind("my_queue", "my_exchange", "my_routing_key", null);
        }
    }
}

// ReSharper restore InconsistentNaming