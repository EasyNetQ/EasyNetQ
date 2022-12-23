using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using FluentAssertions;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EasyNetQ.IntegrationTests.Rpc
{
    [Collection("RabbitMQ")]
    public class When_request_and_respond_with_configured_queues : IDisposable
    {
        public When_request_and_respond_with_configured_queues(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1;timeout=-1");
            this.fixture = fixture;
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        private readonly IBus bus;
        private readonly RabbitMQFixture fixture;

        [Fact]
        public async Task Should_create_classic_queues_by_defualt()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            using (await bus.Rpc.RespondAsync<RabbitRequest, RabbitResponse>((x, ct) =>
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

            string destinationQueueName =
                bus.Advanced.Conventions.RpcRoutingKeyNamingConvention.Invoke(typeof(RabbitRequest));

            Exception e =
                Record.Exception(() => bus.Advanced.QueueDeclare(destinationQueueName, c => c.WithQueueType("quorum")));

            e.Should().BeOfType<OperationInterruptedException>();
            e.Message.Should().Contain("inequivalent arg 'x-queue-type' for queue");
        }

        [Fact]
        public async Task Should_create_quorum_queues_if_requested()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            using (await bus.Rpc.RespondAsync<BunnyRequest, BunnyResponse>((x, ct) =>
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

            string destinationQueueName =
                bus.Advanced.Conventions.RpcRoutingKeyNamingConvention.Invoke(typeof(BunnyRequest));

            Exception e =
                Record.Exception(() => bus.Advanced.QueueDeclare(destinationQueueName, c => c.WithQueueType("classic")));

            e.Should().BeOfType<OperationInterruptedException>();
            e.Message.Should().Contain("inequivalent arg 'x-queue-type' for queue");
        }
    }
}
