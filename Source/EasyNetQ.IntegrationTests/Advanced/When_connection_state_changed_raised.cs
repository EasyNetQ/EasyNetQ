using EasyNetQ.IntegrationTests.Utils;
using EasyNetQ.Management.Client;
using EasyNetQ.Persistent;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Advanced;

[Collection("RabbitMQ")]
public class When_connection_state_changed_raised : IDisposable
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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

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

            var producerStatus = advanced.GetConnectionStatus(PersistentConnectionType.Producer);
            var consumerStatus = advanced.GetConnectionStatus(PersistentConnectionType.Consumer);

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
}
