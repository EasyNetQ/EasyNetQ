using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Rpc;

[Collection("RabbitMQ")]
public class When_request_and_respond_in_flight_during_shutdown : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_request_and_respond_in_flight_during_shutdown(RabbitMQFixture fixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={fixture.Host};prefetchCount=1;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
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
#pragma warning disable CS4014
        Task.Run(() => responder.Dispose(), cts.Token);
#pragma warning restore CS4014

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await requestTask);
        cts.IsCancellationRequested.Should().BeTrue();
    }

    public void Dispose()
    {
        serviceProvider?.Dispose();
    }
}
