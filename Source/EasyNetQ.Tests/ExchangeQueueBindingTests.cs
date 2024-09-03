using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using Queue = EasyNetQ.Topology.Queue;

namespace EasyNetQ.Tests;

public class When_a_queue_is_declared : IDisposable
{
    public When_a_queue_is_declared()
    {
        mockBuilder = new MockBuilder();

        queue = mockBuilder.Bus.Advanced.QueueDeclare(
            queue: "my_queue",
            durable: false,
            exclusive: true,
            autoDelete: true,
            arguments: new Dictionary<string, object>()
                .WithMessageTtl(TimeSpan.FromSeconds(1))
                .WithExpires(TimeSpan.FromSeconds(2))
                .WithMaxPriority(10)
        );
    }

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    private readonly MockBuilder mockBuilder;
    private readonly Queue queue;

    [Fact]
    public async Task Should_declare_the_queue()
    {
        await mockBuilder.Channels[0].Received().QueueDeclareAsync(
            Arg.Is("my_queue"),
            Arg.Is(false),
            Arg.Is(true),
            Arg.Is(true),
            Arg.Is<IDictionary<string, object>>(
                args => (int)args["x-message-ttl"] == 1000 &&
                        (int)args["x-expires"] == 2000 &&
                        (byte)args["x-max-priority"] == 10
            )
        );
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

        var advancedBus = mockBuilder.Bus.Advanced;
        queue = advancedBus.QueueDeclare(
            queue: "my_queue",
            durable: false,
            exclusive: true,
            autoDelete: true,
            arguments: new Dictionary<string, object>()
                .WithMessageTtl(TimeSpan.FromSeconds(1))
                .WithExpires(TimeSpan.FromSeconds(2))
                .WithMaxPriority(10)
                .WithDeadLetterExchange("my_exchange")
                .WithDeadLetterRoutingKey("my_routing_key")
        );
    }

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    private readonly MockBuilder mockBuilder;
    private readonly Queue queue;

    [Fact]
    public async Task Should_declare_the_queue()
    {
        await mockBuilder.Channels[0].Received().QueueDeclareAsync(
            Arg.Is("my_queue"),
            Arg.Is(false),
            Arg.Is(true),
            Arg.Is(true),
            Arg.Is<IDictionary<string, object>>(
                args => (int)args["x-message-ttl"] == 1000 &&
                        (int)args["x-expires"] == 2000 &&
                        (byte)args["x-max-priority"] == 10 &&
                        (string)args["x-dead-letter-exchange"] == "my_exchange" &&
                        (string)args["x-dead-letter-routing-key"] == "my_routing_key"
            )
        );
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

        queue = mockBuilder.Bus.Advanced.QueueDeclare(
            queue: "my_queue",
            durable: false,
            exclusive: true,
            autoDelete: true,
            arguments: new Dictionary<string, object>()
                .WithMessageTtl(TimeSpan.FromSeconds(1))
                .WithExpires(TimeSpan.FromSeconds(2))
                .WithMaxPriority(10)
                .WithDeadLetterExchange(Exchange.DefaultName)
                .WithDeadLetterRoutingKey("my_queue2")
        );
    }

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    private readonly MockBuilder mockBuilder;
    private readonly Queue queue;

    [Fact]
    public async Task Should_declare_the_queue()
    {
        await mockBuilder.Channels[0].Received().QueueDeclareAsync(
            Arg.Is("my_queue"),
            Arg.Is(false),
            Arg.Is(true),
            Arg.Is(true),
            Arg.Is<IDictionary<string, object>>(
                args => (int)args["x-message-ttl"] == 1000 &&
                        (int)args["x-expires"] == 2000 &&
                        (byte)args["x-max-priority"] == 10 &&
                        (string)args["x-dead-letter-exchange"] == "" &&
                        (string)args["x-dead-letter-routing-key"] == "my_queue2"
            )
        );
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

        mockBuilder.Bus.Advanced.QueueDelete("my_queue");
    }

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    private readonly MockBuilder mockBuilder;

    [Fact]
    public async Task Should_delete_the_queue()
    {
        await mockBuilder.Channels[0].Received().QueueDeleteAsync("my_queue", false, false);
    }
}

public class When_a_queue_is_deleted_with_name : IDisposable
{
    public When_a_queue_is_deleted_with_name()
    {
        mockBuilder = new MockBuilder();

        mockBuilder.Bus.Advanced.QueueDelete("my_queue");
    }

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    private readonly MockBuilder mockBuilder;

    [Fact]
    public async Task Should_delete_the_queue()
    {
        await mockBuilder.Channels[0].Received().QueueDeleteAsync("my_queue", false, false);
    }
}

public class When_an_exchange_is_declared : IDisposable
{
    public When_an_exchange_is_declared()
    {
        mockBuilder = new MockBuilder();

        exchange = mockBuilder.Bus.Advanced.ExchangeDeclare(
            "my_exchange",
            type: ExchangeType.Direct,
            durable: false,
            autoDelete: true,
            arguments: new Dictionary<string, object>().WithAlternateExchange("my.alternate.exchange")
        );
    }

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    private readonly MockBuilder mockBuilder;
    private readonly Exchange exchange;

    [Fact]
    public async Task Should_declare_an_exchange()
    {
        await mockBuilder.Channels[0].Received().ExchangeDeclareAsync(
            Arg.Is("my_exchange"),
            Arg.Is("direct"),
            Arg.Is(false),
            Arg.Is(true),
            Arg.Is<IDictionary<string, object>>(c => (string)c["alternate-exchange"] == "my.alternate.exchange")
        );
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

        mockBuilder.Bus.Advanced.ExchangeDeclarePassive("my_exchange");
    }

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    private readonly MockBuilder mockBuilder;

    [Fact]
    public async Task Should_passively_declare_exchange()
    {
        mockBuilder.Channels.Count.Should().Be(1);
        await mockBuilder.Channels[0].Received().ExchangeDeclarePassiveAsync(Arg.Is("my_exchange"));
    }
}

public class When_an_exchange_is_deleted : IDisposable
{
    public When_an_exchange_is_deleted()
    {
        mockBuilder = new MockBuilder();

        var exchange = new Exchange("my_exchange");
        mockBuilder.Bus.Advanced.ExchangeDelete(exchange);
    }

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    private readonly MockBuilder mockBuilder;

    [Fact]
    public async Task Should_delete_the_queue()
    {
        await mockBuilder.Channels[0].Received().ExchangeDeleteAsync("my_exchange", false);
    }
}

public class When_a_queue_is_bound_to_an_exchange : IDisposable
{
    public When_a_queue_is_bound_to_an_exchange()
    {
        mockBuilder = new MockBuilder();
        advancedBus = mockBuilder.Bus.Advanced;

        var exchange = new Exchange("my_exchange");
        var queue = new Queue("my_queue", false);

        binding = advancedBus.Bind(exchange, queue, "my_routing_key");
    }

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    private readonly MockBuilder mockBuilder;
    private readonly IAdvancedBus advancedBus;
    private readonly Binding<Queue> binding;

    [Fact]
    public void Should_create_a_binding_instance()
    {
        binding.Should().NotBeNull();
        binding.RoutingKey.Should().Be("my_routing_key");
        binding.Source.Name.Should().Be("my_exchange");
        binding.Destination.Should().BeAssignableTo<Queue>();
        binding.Destination.Name.Should().Be("my_queue");
    }

    [Fact]
    public async Task Should_declare_a_binding()
    {
        await mockBuilder.Channels[0].Received().QueueBindAsync(
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
        var queue = new Queue("my_queue", false);

        binding = advancedBus.Bind(exchange, queue, "my_routing_key", new Dictionary<string, object> { ["header1"] = "value1" });
    }

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    private readonly MockBuilder mockBuilder;
    private readonly IAdvancedBus advancedBus;
    private readonly Binding<Queue> binding;

    [Fact]
    public void Should_create_a_binding_instance()
    {
        binding.Should().NotBeNull();
        binding.RoutingKey.Should().Be("my_routing_key");
        binding.Source.Name.Should().Be("my_exchange");
        binding.Arguments["header1"].Should().Be("value1");
        binding.Destination.Should().BeAssignableTo<Queue>();
        binding.Destination.Name.Should().Be("my_queue");
    }

    [Fact]
    public async Task Should_declare_a_binding()
    {
        var expectedHeaders = new Dictionary<string, object> { ["header1"] = "value1" };

        await mockBuilder.Channels[0].Received().QueueBindAsync(
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

    public virtual void Dispose()
    {
        mockBuilder.Dispose();
    }

    private readonly MockBuilder mockBuilder;
    private readonly IAdvancedBus advancedBus;
    private readonly Binding<Queue> binding;

    [Fact]
    public async Task Should_unbind_the_exchange()
    {
        await mockBuilder.Channels[0].Received().QueueUnbindAsync("my_queue", "my_exchange", "my_routing_key", null);
    }
}
