using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.Rpc
{
    [Collection("RabbitMQ")]
    public class When_request_and_respond_with_legacy_options : IDisposable
    {
        public When_request_and_respond_with_legacy_options(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1;timeout=5", c => c.EnableLegacyConventions());
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        private readonly IBus bus;

        [Fact]
        public async Task Should_receive_exception()
        {
            using (bus.RespondAsync<Request, Response>(x =>
                Task.FromException<Response>(new RequestFailedException("Oops"))))
            {
                var exception = await Assert.ThrowsAsync<EasyNetQResponderException>(
                    () => bus.RequestAsync<Request, Response>(new Request(42))
                ).ConfigureAwait(false);
                exception.Message.Should().Be("Oops");
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
