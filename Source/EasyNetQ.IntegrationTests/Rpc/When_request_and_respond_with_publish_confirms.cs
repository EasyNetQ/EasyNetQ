using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Rpc;

[Collection("RabbitMQ")]
public class When_request_and_respond_with_publish_confirms : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_request_and_respond_with_publish_confirms(RabbitMQFixture fixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={fixture.Host};prefetchCount=1;publisherConfirms=True;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public virtual void Dispose()
    {
        serviceProvider?.Dispose();
    }

    [Fact]
    public async Task Should_receive_exception()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        using (
            await bus.Rpc.RespondAsync<Request, Response>(
                _ => Task.FromException<Response>(new RequestFailedException()), cts.Token
            )
        )
        {
            await Assert.ThrowsAsync<EasyNetQResponderException>(
                () => bus.Rpc.RequestAsync<Request, Response>(new Request(42), cts.Token)
            );
        }
    }

    [Fact]
    public async Task Should_receive_response()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        using (await bus.Rpc.RespondAsync<Request, Response>(x => new Response(x.Id), cts.Token))
        {
            var response = await bus.Rpc.RequestAsync<Request, Response>(new Request(42), cts.Token);
            response.Should().Be(new Response(42));
        }
    }
}
