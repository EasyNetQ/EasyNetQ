using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using Queue = EasyNetQ.Topology.Queue;

namespace EasyNetQ.Tests;

public class When_a_queue_is_declared : IAsyncLifetime
{
    public When_a_queue_is_declared()
    {
        mockBuilder = new MockBuilder();
    }

    public async Task InitializeAsync()
    {
        queue = await mockBuilder.Bus.Advanced.QueueDeclareAsync(
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


    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private readonly MockBuilder mockBuilder;
    private Queue queue;

    [Fact]
    public async Task Should_declare_the_queue()
    {
        await mockBuilder.Channels[0].Received().QueueDeclareAsync(
            Arg.Is("my_queue"),
            Arg.Is(false),
            Arg.Is(true),
            Arg.Is(true),
            Arg.Is<IDictionary<string, object>>(
                args => (int)args[Argument.MessageTtl] == 1000 &&
                        (int)args[Argument.Expires] == 2000 &&
                        (byte)args[Argument.MaxPriority] == 10
            ),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public void Should_return_a_queue()
    {
        queue.Should().NotBeNull();
        queue.Name.Should().Be("my_queue");
    }
}

public class When_a_queue_is_declared_With_NonEmptyDeadLetterExchange : IAsyncLifetime
{
    public When_a_queue_is_declared_With_NonEmptyDeadLetterExchange()
    {
        mockBuilder = new MockBuilder();

    }

    public async Task InitializeAsync()
    {
        var advancedBus = mockBuilder.Bus.Advanced;
        queue = await advancedBus.QueueDeclareAsync(
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

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private readonly MockBuilder mockBuilder;
    private Queue queue;

    [Fact]
    public async Task Should_declare_the_queue()
    {
        await mockBuilder.Channels[0].Received().QueueDeclareAsync(
            Arg.Is("my_queue"),
            Arg.Is(false),
            Arg.Is(true),
            Arg.Is(true),
            Arg.Is<IDictionary<string, object>>(
                args => (int)args[Argument.MessageTtl] == 1000 &&
                        (int)args[Argument.Expires] == 2000 &&
                        (byte)args[Argument.MaxPriority] == 10 &&
                        (string)args[Argument.DeadLetterExchange] == "my_exchange" &&
                        (string)args[Argument.DeadLetterRoutingKey] == "my_routing_key"
            ),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public void Should_return_a_queue()
    {
        queue.Should().NotBeNull();
        queue.Name.Should().Be("my_queue");
    }
}

public class When_a_queue_is_declared_With_EmptyDeadLetterExchange : IAsyncLifetime
{
    public When_a_queue_is_declared_With_EmptyDeadLetterExchange()
    {
        mockBuilder = new MockBuilder();
    }

    public async Task InitializeAsync()
    {
        queue = await mockBuilder.Bus.Advanced.QueueDeclareAsync(
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


    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private readonly MockBuilder mockBuilder;
    private Queue queue;

    [Fact]
    public async Task Should_declare_the_queue()
    {
        await mockBuilder.Channels[0].Received().QueueDeclareAsync(
            Arg.Is("my_queue"),
            Arg.Is(false),
            Arg.Is(true),
            Arg.Is(true),
            Arg.Is<IDictionary<string, object>>(
                args => (int)args[Argument.MessageTtl] == 1000 &&
                        (int)args[Argument.Expires] == 2000 &&
                        (byte)args[Argument.MaxPriority] == 10 &&
                        (string)args[Argument.DeadLetterExchange] == "" &&
                        (string)args[Argument.DeadLetterRoutingKey] == "my_queue2"
            ),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public void Should_return_a_queue()
    {
        queue.Should().NotBeNull();
        queue.Name.Should().Be("my_queue");
    }
}

public class When_a_queue_is_deleted : IAsyncLifetime
{
    public When_a_queue_is_deleted()
    {
        mockBuilder = new MockBuilder();

    }

    public Task InitializeAsync() => mockBuilder.Bus.Advanced.QueueDeleteAsync("my_queue");

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private readonly MockBuilder mockBuilder;

    [Fact]
    public async Task Should_delete_the_queue()
    {
        await mockBuilder.Channels[0].Received().QueueDeleteAsync("my_queue", false, false);
    }
}

public class When_a_queue_is_deleted_with_name : IAsyncLifetime
{
    public When_a_queue_is_deleted_with_name()
    {
        mockBuilder = new MockBuilder();


    }

    public Task InitializeAsync() => mockBuilder.Bus.Advanced.QueueDeleteAsync("my_queue");

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private readonly MockBuilder mockBuilder;

    [Fact]
    public async Task Should_delete_the_queue()
    {
        await mockBuilder.Channels[0].Received().QueueDeleteAsync("my_queue", false, false);
    }
}

public class When_an_exchange_is_declared : IAsyncLifetime
{
    public When_an_exchange_is_declared()
    {
        mockBuilder = new MockBuilder();


    }

    public async Task InitializeAsync()
    {
        exchange = await mockBuilder.Bus.Advanced.ExchangeDeclareAsync(
    "my_exchange",
    type: ExchangeType.Direct,
    durable: false,
    autoDelete: true,
    arguments: new Dictionary<string, object>().WithAlternateExchange("my.alternate.exchange")
);
    }

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private readonly MockBuilder mockBuilder;
    private Exchange exchange;

    [Fact]
    public async Task Should_declare_an_exchange()
    {
        await mockBuilder.Channels[0].Received().ExchangeDeclareAsync(
            Arg.Is("my_exchange"),
            Arg.Is(ExchangeType.Direct),
            Arg.Is(false),
            Arg.Is(true),
            Arg.Is<IDictionary<string, object>>(c => (string)c["alternate-exchange"] == "my.alternate.exchange"),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public void Should_return_an_exchange_instance()
    {
        exchange.Should().NotBeNull();
        exchange.Name.Should().Be("my_exchange");
    }
}

public class When_an_exchange_is_declared_passively : IAsyncLifetime
{
    public When_an_exchange_is_declared_passively()
    {
        mockBuilder = new MockBuilder();


    }

    public Task InitializeAsync() => mockBuilder.Bus.Advanced.ExchangeDeclarePassiveAsync("my_exchange");

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private readonly MockBuilder mockBuilder;

    [Fact]
    public async Task Should_passively_declare_exchange()
    {
        mockBuilder.Channels.Count.Should().Be(1);
        await mockBuilder.Channels[0].Received().ExchangeDeclarePassiveAsync(Arg.Is("my_exchange"));
    }
}

public class When_an_exchange_is_deleted : IAsyncLifetime
{
    public When_an_exchange_is_deleted()
    {
        mockBuilder = new MockBuilder();


    }

    public async Task InitializeAsync()
    {
        var exchange = new Exchange("my_exchange");
        await mockBuilder.Bus.Advanced.ExchangeDeleteAsync(exchange);
    }

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private readonly MockBuilder mockBuilder;

    [Fact]
    public async Task Should_delete_the_queue()
    {
        await mockBuilder.Channels[0].Received().ExchangeDeleteAsync("my_exchange", false);
    }
}

public class When_a_queue_is_bound_to_an_exchange : IAsyncLifetime
{
    public When_a_queue_is_bound_to_an_exchange()
    {
        mockBuilder = new MockBuilder();
        advancedBus = mockBuilder.Bus.Advanced;
    }

    public async Task InitializeAsync()
    {
        var exchange = new Exchange("my_exchange");
        var queue = new Queue("my_queue", false);
        binding = await advancedBus.BindAsync(exchange, queue, "my_routing_key");
    }

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private readonly MockBuilder mockBuilder;
    private readonly IAdvancedBus advancedBus;
    private Binding<Queue> binding;

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
            Arg.Is((IDictionary<string, object>)null),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );
    }
}

public class When_a_queue_is_bound_to_an_exchange_with_headers : IAsyncLifetime
{
    public When_a_queue_is_bound_to_an_exchange_with_headers()
    {
        mockBuilder = new MockBuilder();
        advancedBus = mockBuilder.Bus.Advanced;
    }

    public async Task InitializeAsync()
    {

        var exchange = new Exchange("my_exchange");
        var queue = new Queue("my_queue", false);

        binding = await advancedBus.BindAsync(exchange, queue, "my_routing_key", new Dictionary<string, object> { ["header1"] = "value1" });
    }

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private readonly MockBuilder mockBuilder;
    private readonly IAdvancedBus advancedBus;
    private Binding<Queue> binding;

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
            Arg.Is<Dictionary<string, object>>(x => x.SequenceEqual(expectedHeaders)),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );
    }
}

public class When_a_queue_is_unbound_from_an_exchange : IAsyncLifetime
{
    public When_a_queue_is_unbound_from_an_exchange()
    {
        mockBuilder = new MockBuilder();
        advancedBus = mockBuilder.Bus.Advanced;
    }

    public async Task InitializeAsync()
    {

        var exchange = new Exchange("my_exchange");
        var queue = new Queue("my_queue", false);
        binding = await advancedBus.BindAsync(exchange, queue, "my_routing_key");
        await advancedBus.UnbindAsync(binding);
    }

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    private readonly MockBuilder mockBuilder;
    private readonly IAdvancedBus advancedBus;
    private Binding<Queue> binding;

    [Fact]
    public async Task Should_unbind_the_exchange()
    {
        await mockBuilder.Channels[0].Received().QueueUnbindAsync("my_queue", "my_exchange", "my_routing_key", null);
    }
}
