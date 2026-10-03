using EasyNetQ.Configuration;
using EasyNetQ.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyNetQ.Core.Tests;

public class BindExistingTests
{
    public sealed record FileEvent(long FileId);

    [Fact]
    public async Task Should_bind_to_an_existing_exchange_without_declaring_it()
    {
        var received = new TaskCompletionSource<FileEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport = new StrictTopologyTransport();
        transport.DeclareExternally("nextcloud.events");
        var services = new ServiceCollection();
        services.AddSingleton<ITransport>(transport);
        services.AddEasyNetQCore().Consume(c => c
            .Queue("vloer.nextcloud-events")
            .BindExisting("nextcloud.events", "file.#")
            .Handle<FileEvent>((message, _) =>
            {
                received.TrySetResult(message);
                return new ValueTask<AckDecision>(AckDecision.Ack);
            }));

        await using var provider = services.BuildServiceProvider();
        var host = provider.GetServices<IHostedService>().Single();
        await host.StartAsync(TestContext.Current.CancellationToken);

        await WireNameTests.PublishRawAsync(transport.Inner, provider, "nextcloud.events", "file.deleted", null, new FileEvent(42));

        (await received.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).Should().Be(new FileEvent(42));
        transport.DeclaredExchanges.Should().BeEmpty();
        await host.StopAsync(TestContext.Current.CancellationToken);
    }
}
