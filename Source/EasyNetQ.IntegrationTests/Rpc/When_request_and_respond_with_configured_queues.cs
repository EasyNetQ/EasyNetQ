using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.IntegrationTests.Rpc
{
    [Collection("RabbitMQ")]
    public class When_request_and_respond_with_configured_queues : IAsyncLifetime
    {
        private readonly ServiceProvider serviceProvider;
        private readonly IBus bus;
        private readonly RabbitMQFixture fixture;
        readonly Conventions conventions = new Conventions(new DefaultTypeNameSerializer());

        public When_request_and_respond_with_configured_queues(RabbitMQFixture fixture)
        {

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddEasyNetQ($"host={fixture.Host};prefetchCount=1;timeout=-1");

            var serviceProvider = serviceCollection.BuildServiceProvider();
            bus = serviceProvider.GetRequiredService<IBus>();
            this.fixture = fixture;
        }


        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            if (serviceProvider != null)
                await serviceProvider.DisposeAsync();
        }

        [Fact]
        public async Task Should_create_classic_queues_by_defualt()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await using (await bus.Rpc.RespondAsync<RabbitRequest, RabbitResponse>((x, ct) =>
            {
                return x switch
                {
                    RabbitRequest b => Task.FromResult(new RabbitResponse(b.Id)),
                    _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
                };
            },
            c => { },
            cts.Token))
            {
                var bunnyResponse = await bus.Rpc.RequestAsync<RabbitRequest, RabbitResponse>(
                    new RabbitRequest(42), cts.Token
                );
                bunnyResponse.Should().Be(new RabbitResponse(42));
            }

            string destinationQueueName = conventions.RpcRoutingKeyNamingConvention.Invoke(typeof(RabbitRequest));

            Exception e =
                await Record.ExceptionAsync(() => bus.Advanced.QueueDeclareAsync(destinationQueueName, c => c.WithQueueType("quorum")));

            e.Should().BeOfType<OperationInterruptedException>();
            e.Message.Should().Contain("inequivalent arg 'x-queue-type' for queue");
        }

        [Fact]
        public async Task Should_create_quorum_queues_if_requested()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await using (await bus.Rpc.RespondAsync<BunnyRequest, BunnyResponse>((x, ct) =>
            {
                return x switch
                {
                    BunnyRequest b => Task.FromResult(new BunnyResponse(b.Id)),
                    _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
                };
            },
            c => c.WithQueueType("quorum"),
            cts.Token))
            {
                var bunnyResponse = await bus.Rpc.RequestAsync<BunnyRequest, BunnyResponse>(
                    new BunnyRequest(42), c => c.WithQueueType("quorum"), cts.Token
                );
                bunnyResponse.Should().Be(new BunnyResponse(42));
            }

            string destinationQueueName = conventions.RpcRoutingKeyNamingConvention.Invoke(typeof(BunnyRequest));

            Exception e =
                await Record.ExceptionAsync(() => bus.Advanced.QueueDeclareAsync(destinationQueueName, c => c.WithQueueType("classic")));

            e.Should().BeOfType<OperationInterruptedException>();
            e.Message.Should().Contain("inequivalent arg 'x-queue-type' for queue");
        }

        [Fact]
        public async Task Should_use_attribure_queues_by_defualt()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await using (await bus.Rpc.RespondAsync<RabbitQuorumRequest, RabbitQuorumResponse>((x, ct) =>
            {
                return x switch
                {
                    RabbitQuorumRequest b => Task.FromResult(new RabbitQuorumResponse(b.Id)),
                    _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
                };
            },
            c => { },
            cts.Token))
            {
                var bunnyResponse = await bus.Rpc.RequestAsync<RabbitQuorumRequest, RabbitQuorumResponse>(
                    new RabbitQuorumRequest(42), cts.Token
                );
                bunnyResponse.Should().Be(new RabbitQuorumResponse(42));
            }
        }
    }
}
