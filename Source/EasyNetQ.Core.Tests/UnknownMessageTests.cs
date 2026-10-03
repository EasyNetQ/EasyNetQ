using System.Text;
using EasyNetQ.Configuration;
using EasyNetQ.Consumer;
using EasyNetQ.Pipeline;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyNetQ.Core.Tests;

public class UnknownMessageTests
{
    public sealed record OrderPlaced(int Id);
    public sealed record OrderShipped(int Id);
    public sealed record NotHandled(int Id);

    private sealed class CapturingErrorStrategy : IConsumeErrorStrategy
    {
        public readonly TaskCompletionSource<Exception> Error = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<AckDecision> HandleErrorAsync(ConsumeContext context, Exception exception, CancellationToken cancellationToken = default)
        {
            Error.TrySetResult(exception);
            return new ValueTask<AckDecision>(AckDecision.NackDiscard);
        }

        public ValueTask<AckDecision> HandleCancelledAsync(ConsumeContext context, CancellationToken cancellationToken = default)
            => new(AckDecision.NackRequeue);
    }

    private static async Task<(ServiceProvider Provider, InMemoryTransport Transport, IHostedService Host)> StartAsync(
        Action<GenericConsumerBuilder> configure,
        IConsumeErrorStrategy? errorStrategy = null
    )
    {
        var transport = new InMemoryTransport();
        var services = new ServiceCollection();
        services.AddSingleton<ITransport>(transport);
        if (errorStrategy is not null)
            services.AddSingleton(errorStrategy);
        services.AddEasyNetQCore().Consume(c => configure(c.Queue("q")));
        var provider = services.BuildServiceProvider();
        var host = provider.GetServices<IHostedService>().Single();
        await host.StartAsync(TestContext.Current.CancellationToken);
        return (provider, transport, host);
    }

    [Fact]
    public async Task Should_hand_a_foreign_wire_name_to_the_unknown_handler_as_raw_bytes()
    {
        var received = new TaskCompletionSource<(string Body, string? Type)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var (provider, transport, host) = await StartAsync(c => c
            .Handle<OrderPlaced>((_, _) => new ValueTask<AckDecision>(AckDecision.Ack))
            .HandleUnknown((body, context) =>
            {
                received.TrySetResult((Encoding.UTF8.GetString(body.Span), context.Properties.Type));
                return new ValueTask<AckDecision>(AckDecision.Ack);
            }));

        await WireNameTests.PublishRawAsync(transport, provider, "", "q", "Tracer.Plugins.Slack.Events.SlackChat", new { text = "hi" });

        var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        message.Should().Be(("{\"text\":\"hi\"}", "Tracer.Plugins.Slack.Events.SlackChat"));
        await host.StopAsync(TestContext.Current.CancellationToken);
        await provider.DisposeAsync();
    }

    [Fact]
    public async Task Should_route_a_known_type_without_handler_to_the_unknown_handler_without_deserializing()
    {
        var received = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var (provider, transport, host) = await StartAsync(c => c
            .Handle<OrderPlaced>((_, _) => new ValueTask<AckDecision>(AckDecision.Ack))
            .HandleUnknown((_, context) =>
            {
                received.TrySetResult(context.Properties.Type);
                return new ValueTask<AckDecision>(AckDecision.Ack);
            }));
        var wireName = provider.GetRequiredService<IMessageTypeRegistry>().GetOrAdd<NotHandled>().WireName;

        // not valid JSON for NotHandled: proves the body is not deserialized for the raw handler
        await WireNameTests.PublishRawAsync(transport, provider, "", "q", wireName, "not an object");

        (await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Should().Be(wireName);
        await host.StopAsync(TestContext.Current.CancellationToken);
        await provider.DisposeAsync();
    }

    [Fact]
    public async Task Should_fail_an_unresolvable_wire_name_with_a_typed_exception()
    {
        var strategy = new CapturingErrorStrategy();
        var (provider, transport, host) = await StartAsync(
            c => c.Handle<OrderPlaced>((_, _) => new ValueTask<AckDecision>(AckDecision.Ack)),
            strategy
        );

        await WireNameTests.PublishRawAsync(transport, provider, "", "q", "Some.Foreign.Type", new { });

        var error = await strategy.Error.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        error.Should().BeOfType<UnknownMessageTypeException>().Which.WireName.Should().Be("Some.Foreign.Type");
        await host.StopAsync(TestContext.Current.CancellationToken);
        await provider.DisposeAsync();
    }
}
