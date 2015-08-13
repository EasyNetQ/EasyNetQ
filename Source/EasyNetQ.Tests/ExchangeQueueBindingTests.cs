using System.Collections.Generic;
// ReSharper disable InconsistentNaming
using System.Collections;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using NUnit.Framework;
using Rhino.Mocks;

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

        [Test]
        public void Should_return_a_queue()
        {
            queue.ShouldNotBeNull();
            queue.Name.ShouldEqual("my_queue");
        }

        [Test]
        public void Should_declare_the_queue()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => 
                x.QueueDeclare(
                    Arg<string>.Is.Equal("my_queue"), 
                    Arg<bool>.Is.Equal(false),
                    Arg<bool>.Is.Equal(true),
                    Arg<bool>.Is.Equal(true),
                    Arg<IDictionary<string, object>>.Matches(args => 
                        ((int)args["x-message-ttl"] == 1000) &&
                        ((int)args["x-expires"] == 2000) &&
                        ((int)args["x-max-priority"] == 10))));
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

        [Test]
        public void Should_delete_the_queue()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => x.QueueDelete("my_queue", false, false));
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

            mockBuilder.NextModel.Stub(x => x.ExchangeDeclare(null, null, false, false, null))
                .IgnoreArguments()
                .WhenCalled(x =>
                    {
                        arguments = x.Arguments[4] as IDictionary;
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

        [Test]
        public void Should_return_an_exchange_instance()
        {
            exchange.ShouldNotBeNull();
            exchange.Name.ShouldEqual("my_exchange");
        }

        [Test]
        public void Should_declare_an_exchange()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => 
                x.ExchangeDeclare(
                    Arg<string>.Is.Equal("my_exchange"),
                    Arg<string>.Is.Equal("direct"),
                    Arg<bool>.Is.Equal(false),
                    Arg<bool>.Is.Equal(true),
                    Arg<IDictionary<string, object>>.Is.Anything));
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
            mockBuilder.Channels[0].AssertWasCalled(x =>
                x.ExchangeDeclarePassive(Arg<string>.Is.Equal("my_exchange")));
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

        [Test]
        public void Should_delete_the_queue()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => x.ExchangeDelete("my_exchange", false));
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
            mockBuilder.Channels[0].AssertWasCalled(x => 
                x.QueueBind("my_queue", "my_exchange", "my_routing_key"));
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

        [Test]
        public void Should_unbind_the_exchange()
        {
            mockBuilder.Channels[0].AssertWasCalled(x => 
                x.QueueUnbind("my_queue", "my_exchange", "my_routing_key", null));
        }
    }
}

// ReSharper restore InconsistentNaming