using System;
using System.Threading;
using System.Threading.Tasks;
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
            bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1");
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
            using (bus.RespondAsync<Request, Response>(x => Task.FromResult(new Response(x.Id))))
            {
                await bus.RequestAsync<Request, Response>(new Request(42)).ConfigureAwait(false);

                await rmqFixture.RestartAsync(CancellationToken.None).ConfigureAwait(false);

                // The crunch to deal with the race when Rpc has not handled reconnection yet
                try
                {
                    await bus.RequestAsync<Request, Response>(new Request(42)).ConfigureAwait(false);
                }
                catch (EasyNetQException)
                {
                }

                await bus.RequestAsync<Request, Response>(new Request(42)).ConfigureAwait(false);
            }
        }
    }
}
