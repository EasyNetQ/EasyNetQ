using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.Rpc
{
    [Collection("RabbitMQ")]
    public class When_request_and_respond_with_default_options : IDisposable
    {
        private readonly RabbitMQFixture rmqFixture;

        public When_request_and_respond_with_default_options(RabbitMQFixture rmqFixture)
        {
            this.rmqFixture = rmqFixture;
            bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1;timeout=5");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        private readonly IBus bus;

        [Fact]
        public async Task Should_receive_exception()
        {
            using (bus.RespondAsync<Request, Response>(x => Task.FromException<Response>(new RequestFailedException())))
            {
                await Assert.ThrowsAsync<EasyNetQResponderException>(
                    () => bus.RequestAsync<Request, Response>(new Request(42))
                ).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task Should_receive_response()
        {
            using (bus.RespondAsync<Request, Response>(x => Task.FromResult(new Response(x.Id))))
            {
                var response = await bus.RequestAsync<Request, Response>(new Request(42)).ConfigureAwait(false);
                response.Should().Be(new Response(42));
            }
        }

        [Fact]
        public async Task Should_survive_restart()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            using (bus.RespondAsync<Request, Response>(x => Task.FromResult(new Response(x.Id))))
            {
                await bus.RequestAsync<Request, Response>(new Request(42)).ConfigureAwait(false);

                await rmqFixture.ManagementClient.KillAllConnectionsAsync(cts.Token);

                try
                {
                    await bus.RequestAsync<Request, Response>(new Request(42)).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // The crunch to deal with the race when Rpc has not handled reconnection yet
                }

                await bus.RequestAsync<Request, Response>(new Request(42)).ConfigureAwait(false);
            }
        }
    }
}
