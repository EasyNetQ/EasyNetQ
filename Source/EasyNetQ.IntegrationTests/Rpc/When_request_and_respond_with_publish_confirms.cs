using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.Rpc
{
    [Collection("RabbitMQ")]
    public class When_request_and_respond_with_publish_confirms : IDisposable
    {
        public When_request_and_respond_with_publish_confirms(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1;publisherConfirms=True;timeout=5");
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
    }
}
