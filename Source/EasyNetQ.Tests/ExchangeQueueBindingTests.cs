// ReSharper disable InconsistentNaming

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Queue = EasyNetQ.Topology.Queue;

namespace EasyNetQ.Tests
{
    public class When_a_queue_is_declared : IDisposable
    {
        public When_a_queue_is_declared()
        {
            mockBuilder = new MockBuilder();

            advancedBus = mockBuilder.Bus.Advanced;
            queue = advancedBus.QueueDeclare(
                "my_queue",
                c => c.AsDurable(false)
                    .AsExclusive(true)
                    .AsAutoDelete(true)
                    .WithMessageTtl(TimeSpan.FromSeconds(1))
                    .WithExpires(TimeSpan.FromSeconds(2))
                    .WithMaxPriority(10)
            );
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;
        private IQueue queue;

        [Fact]
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

        [Fact]
        public void Should_return_a_queue()
        {
            queue.Should().NotBeNull();
            queue.Name.Should().Be("my_queue");
        }
    }

    public class When_a_queue_is_declared_With_NonEmptyDeadLetterExchange : IDisposable
    {
        public When_a_queue_is_declared_With_NonEmptyDeadLetterExchange()
        {
            mockBuilder = new MockBuilder();

            advancedBus = mockBuilder.Bus.Advanced;
            queue = advancedBus.QueueDeclare(
                "my_queue",
                c => c.AsDurable(false)
                    .AsExclusive(true)
                    .AsAutoDelete(true)
                    .WithMessageTtl(TimeSpan.FromSeconds(1))
                    .WithExpires(TimeSpan.FromSeconds(2))
                    .WithMaxPriority(10)
                    .WithDeadLetterExchange(new Exchange("my_exchange"))
                    .WithDeadLetterRoutingKey("my_routing_key")
            );
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;
        private IQueue queue;

        [Fact]
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

        [Fact]
        public void Should_return_a_queue()
        {
            queue.Should().NotBeNull();
            queue.Name.Should().Be("my_queue");
        }
    }

    public class When_a_queue_is_declared_With_EmptyDeadLetterExchange : IDisposable
    {
        public When_a_queue_is_declared_With_EmptyDeadLetterExchange()
        {
            mockBuilder = new MockBuilder();

            advancedBus = mockBuilder.Bus.Advanced;
            queue = advancedBus.QueueDeclare(
                "my_queue",
                c => c.AsDurable(false)
                    .AsExclusive(true)
                    .AsAutoDelete(true)
                    .WithMessageTtl(TimeSpan.FromSeconds(1))
                    .WithExpires(TimeSpan.FromSeconds(2))
                    .WithMaxPriority(10)
                    .WithDeadLetterExchange(Exchange.GetDefault())
                    .WithDeadLetterRoutingKey("my_queue2")
            );
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;
        private IQueue queue;

        [Fact]
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

        [Fact]
        public void Should_return_a_queue()
        {
            queue.Should().NotBeNull();
            queue.Name.Should().Be("my_queue");
        }
    }

    public class When_a_queue_is_deleted : IDisposable
    {
        public When_a_queue_is_deleted()
        {
            mockBuilder = new MockBuilder();
            advancedBus = mockBuilder.Bus.Advanced;

            var queue = new Queue("my_queue", false);
            advancedBus.QueueDelete(queue);
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;

        [Fact]
        public void Should_delete_the_queue()
        {
            mockBuilder.Channels[0].Received().QueueDelete("my_queue", false, false);
        }
    }

    public class When_an_exchange_is_declared : IDisposable
    {
        public When_an_exchange_is_declared()
        {
            mockBuilder = new MockBuilder();
            advancedBus = mockBuilder.Bus.Advanced;

            mockBuilder.NextModel.WhenForAnyArgs(x => x.ExchangeDeclare(null, null, false, false, null))
                .Do(x => { arguments = x[4] as IDictionary; });

            exchange = advancedBus.ExchangeDeclare(
                "my_exchange",
                c => c.WithType(ExchangeType.Direct)
                    .AsDurable(false)
                    .AsAutoDelete(true)
                    .WithAlternateExchange(new Exchange("my.alternate.exchange"))
            );
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;
        private IExchange exchange;
        private IDictionary arguments;

        [Fact]
        public void Should_add_correct_arguments()
        {
            arguments.Should().NotBeNull();
            arguments["alternate-exchange"].Should().Be("my.alternate.exchange");
        }

        [Fact]
        public void Should_declare_an_exchange()
        {
            mockBuilder.Channels[0].Received().ExchangeDeclare(
                Arg.Is("my_exchange"),
                Arg.Is("direct"),
                Arg.Is(false),
                Arg.Is(true),
                Arg.Any<IDictionary<string, object>>());
        }

        [Fact]
        public void Should_return_an_exchange_instance()
        {
            exchange.Should().NotBeNull();
            exchange.Name.Should().Be("my_exchange");
        }
    }

    public class When_an_exchange_is_declared_passively : IDisposable
    {
        public When_an_exchange_is_declared_passively()
        {
            mockBuilder = new MockBuilder();
            advancedBus = mockBuilder.Bus.Advanced;

            advancedBus.ExchangeDeclarePassive("my_exchange");
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;

        [Fact]
        public void Should_passively_declare_exchange()
        {
            mockBuilder.Channels.Count.Should().Be(1);
            mockBuilder.Channels[0].Received().ExchangeDeclarePassive(Arg.Is("my_exchange"));
        }
    }

    public class When_an_exchange_is_deleted : IDisposable
    {
        public When_an_exchange_is_deleted()
        {
            mockBuilder = new MockBuilder();
            advancedBus = mockBuilder.Bus.Advanced;

            var exchange = new Exchange("my_exchange");
            advancedBus.ExchangeDelete(exchange);
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;

        [Fact]
        public void Should_delete_the_queue()
        {
            mockBuilder.Channels[0].Received().ExchangeDelete("my_exchange", false);
        }
    }

    public class When_a_queue_is_bound_to_an_exchange : IDisposable
    {
        public When_a_queue_is_bound_to_an_exchange()
        {
            mockBuilder = new MockBuilder();
            advancedBus = mockBuilder.Bus.Advanced;

            var exchange = new Exchange("my_exchange");
            var queue = new Topology.Queue("my_queue", false);

            binding = advancedBus.Bind(exchange, queue, "my_routing_key");
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;
        private IBinding binding;

        [Fact]
        public void Should_create_a_binding_instance()
        {
            binding.Should().NotBeNull();
            binding.RoutingKey.Should().Be("my_routing_key");
            binding.Exchange.Name.Should().Be("my_exchange");
            binding.Bindable.Should().BeAssignableTo<IQueue>();
            ((IQueue)binding.Bindable).Name.Should().Be("my_queue");
        }

        [Fact]
        public void Should_declare_a_binding()
        {
            mockBuilder.Channels[0].Received().QueueBind(
                Arg.Is("my_queue"),
                Arg.Is("my_exchange"),
                Arg.Is("my_routing_key"),
                Arg.Is((IDictionary<string, object>)null)
            );
        }
    }

    public class When_a_queue_is_bound_to_an_exchange_with_headers : IDisposable
    {
        public When_a_queue_is_bound_to_an_exchange_with_headers()
        {
            mockBuilder = new MockBuilder();
            advancedBus = mockBuilder.Bus.Advanced;

            var exchange = new Exchange("my_exchange");
            var queue = new Topology.Queue("my_queue", false);

            binding = advancedBus.Bind(exchange, queue, "my_routing_key", new Dictionary<string, object> { ["header1"] = "value1" });
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;
        private IBinding binding;

        [Fact]
        public void Should_create_a_binding_instance()
        {
            binding.Should().NotBeNull();
            binding.RoutingKey.Should().Be("my_routing_key");
            binding.Exchange.Name.Should().Be("my_exchange");
            binding.Arguments["header1"].Should().Be("value1");
            binding.Bindable.Should().BeAssignableTo<IQueue>();
            ((IQueue)binding.Bindable).Name.Should().Be("my_queue");
        }

        [Fact]
        public void Should_declare_a_binding()
        {
            var expectedHeaders = new Dictionary<string, object> { ["header1"] = "value1" };

            mockBuilder.Channels[0].Received().QueueBind(
                Arg.Is("my_queue"),
                Arg.Is("my_exchange"),
                Arg.Is("my_routing_key"),
                Arg.Is<Dictionary<string, object>>(x => x.SequenceEqual(expectedHeaders)));
        }
    }

    public class When_a_queue_is_unbound_from_an_exchange : IDisposable
    {
        public When_a_queue_is_unbound_from_an_exchange()
        {
            mockBuilder = new MockBuilder();
            advancedBus = mockBuilder.Bus.Advanced;

            var exchange = new Exchange("my_exchange");
            var queue = new Queue("my_queue", false);
            binding = advancedBus.Bind(exchange, queue, "my_routing_key");
            advancedBus.Unbind(binding);
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private MockBuilder mockBuilder;
        private IAdvancedBus advancedBus;
        private readonly IBinding binding;

        [Fact]
        public void Should_unbind_the_exchange()
        {
            mockBuilder.Channels[0].Received().QueueUnbind("my_queue", "my_exchange", "my_routing_key", null);
        }
    }
}

// ReSharper restore InconsistentNaming
