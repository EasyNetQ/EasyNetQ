using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.Rpc
{
    [Collection("RabbitMQ")]
    public class When_request_and_respond_polymorphic : IDisposable
    {
        public When_request_and_respond_polymorphic(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1;timeout=5");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        private readonly IBus bus;

        [Fact]
        public async Task Should_receive_response()
        {
            using (bus.RespondAsync<Request, Response>(x =>
            {
                return Task.FromResult<Response>(x switch
                {
                    BunnyRequest b => new BunnyResponse(b.Id),
                    RabbitRequest r => new RabbitResponse(r.Id),
                    _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
                });
            }))
            {
                var bunnyResponse = await bus.RequestAsync<Request, Response>(
                    new BunnyRequest(42)
                ).ConfigureAwait(false);
                bunnyResponse.Should().Be(new BunnyResponse(42));

                var rabbitResponse = await bus.RequestAsync<Request, Response>(
                    new RabbitRequest(42)
                ).ConfigureAwait(false);
                rabbitResponse.Should().Be(new RabbitResponse(42));
            }
        }
    }
}
