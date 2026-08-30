using EasyNetQ.IntegrationTests.Utils;
using EasyNetQ.Management.Client;
using EasyNetQ.Persistent;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_connection_state_changed_raised : IDisposable, IAsyncLifetime
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_connection_state_changed_raised(RabbitMQFixture rmqFixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host}");
        managementClient = rmqFixture.ManagementClient;

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
    }

    public virtual void Dispose()
    {
        serviceProvider?.Dispose();
    }

    private readonly IManagementClient managementClient;

    [Fact]
    public async Task Test()
    {
        var advanced = bus.Advanced;

        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var producerStatus = advanced.GetConnectionStatus(PersistentConnectionType.Producer);
            var consumerStatus = advanced.GetConnectionStatus(PersistentConnectionType.Consumer);

            producerStatus.Should().Be(
                new PersistentConnectionStatus(
                    Type: PersistentConnectionType.Producer,
                    State: PersistentConnectionState.NotInitialised
                )
            );
            consumerStatus.Should().Be(
                new PersistentConnectionStatus(
                    Type: PersistentConnectionType.Consumer,
                    State: PersistentConnectionState.NotInitialised
                )
            );
        }

        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            await advanced.EnsureConnectedAsync(PersistentConnectionType.Producer, cts.Token);

            var producerStatus = advanced.GetConnectionStatus(PersistentConnectionType.Producer);
            var consumerStatus = advanced.GetConnectionStatus(PersistentConnectionType.Consumer);

            producerStatus.Should().BeEquivalentTo(
                new PersistentConnectionStatus(
                    Type: PersistentConnectionType.Producer,
                    State: PersistentConnectionState.Connected,
                    ConnectedAt: DateTime.UtcNow
                ),
                c => c.Excluding(x => x.ConnectedAt)
            );
            consumerStatus.Should().Be(
                new PersistentConnectionStatus(
                    Type: PersistentConnectionType.Consumer,
                    State: PersistentConnectionState.NotInitialised
                )
            );
        }

        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            await advanced.EnsureConnectedAsync(PersistentConnectionType.Consumer, cts.Token);

            var producerStatus = advanced.GetConnectionStatus(PersistentConnectionType.Producer);
            var consumerStatus = advanced.GetConnectionStatus(PersistentConnectionType.Consumer);

            producerStatus.Should().BeEquivalentTo(
                new PersistentConnectionStatus(
                    Type: PersistentConnectionType.Producer,
                    State: PersistentConnectionState.Connected,
                    ConnectedAt: DateTime.UtcNow
                ),
                c => c.Excluding(x => x.ConnectedAt)
            );
            consumerStatus.Should().BeEquivalentTo(
                new PersistentConnectionStatus(
                    Type: PersistentConnectionType.Consumer,
                    State: PersistentConnectionState.Connected,
                    ConnectedAt: DateTime.UtcNow
                ),
                c => c.Excluding(x => x.ConnectedAt)
            );
        }

        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await managementClient.KillAllConnectionsAsync(cts.Token);

            // The broker closes the sockets asynchronously and the client recovers automatically a few seconds later,
            // so poll both connections concurrently and capture each status the moment it reports the disconnect
            // (waiting sequentially would let the second connection recover before its poll starts)
            var producerStatusTask = WaitForStateAsync(advanced, PersistentConnectionType.Producer, PersistentConnectionState.Disconnected, cts.Token);
            var consumerStatusTask = WaitForStateAsync(advanced, PersistentConnectionType.Consumer, PersistentConnectionState.Disconnected, cts.Token);
            var producerStatus = await producerStatusTask;
            var consumerStatus = await consumerStatusTask;

            producerStatus.Should().BeEquivalentTo(
                new PersistentConnectionStatus(
                    Type: PersistentConnectionType.Producer,
                    State: PersistentConnectionState.Disconnected,
                    FailureReason: "AMQP close-reason, initiated by Peer, code=320, text='CONNECTION_FORCED - Closed via management plugin', classId=0, methodId=0"
                ),
                c => c.Excluding(x => x.ConnectedAt)
            );
            consumerStatus.Should().BeEquivalentTo(
                new PersistentConnectionStatus(
                    Type: PersistentConnectionType.Consumer,
                    State: PersistentConnectionState.Disconnected,
                    FailureReason: "AMQP close-reason, initiated by Peer, code=320, text='CONNECTION_FORCED - Closed via management plugin', classId=0, methodId=0"
                ),
                c => c.Excluding(x => x.ConnectedAt)
            );
        }
    }

    private static async Task<PersistentConnectionStatus> WaitForStateAsync(
        IAdvancedBus advanced, PersistentConnectionType type, PersistentConnectionState state, CancellationToken cancellationToken
    )
    {
        while (true)
        {
            var status = advanced.GetConnectionStatus(type);
            if (status.State == state) return status;
            await Task.Delay(50, cancellationToken);
        }
    }
}
