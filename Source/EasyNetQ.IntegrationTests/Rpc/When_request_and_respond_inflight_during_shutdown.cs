using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.IntegrationTests.Rpc
{
    [Collection("RabbitMQ")]
    public class When_request_and_respond_in_flight_during_shutdown : IDisposable
    {
        private readonly IBus bus;

        public When_request_and_respond_in_flight_during_shutdown(RabbitMQFixture fixture)
        {
            bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1;timeout=-1");
        }

        [Fact]
        public async Task Should_receive_cancellation()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var requestArrived = new ManualResetEventSlim(false);
            var responder = await bus.Rpc.RespondAsync<Request, Response>(
                async (r, c) =>
                {
                    requestArrived.Set();
                    await Task.Delay(TimeSpan.FromSeconds(2), c);
                    return new Response(r.Id);
                },
                _ => { },
                cts.Token
            );
            var requestTask = bus.Rpc.RequestAsync<Request, Response>(new Request(42), cts.Token);
            requestArrived.Wait(cts.Token);
            Task.Run(() => responder.Dispose(), cts.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await requestTask);
            cts.IsCancellationRequested.Should().BeTrue();
        }

        public void Dispose() => bus.Dispose();
    }
}
