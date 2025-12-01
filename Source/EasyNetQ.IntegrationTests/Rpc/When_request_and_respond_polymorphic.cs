using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Rpc;

[Collection("RabbitMQ")]
public class When_request_and_respond_polymorphic : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_request_and_respond_polymorphic(RabbitMQFixture fixture)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={fixture.Host};prefetchCount=1;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public virtual void Dispose()
    {
        serviceProvider?.Dispose();
    }

    [Fact]
    public async Task Should_receive_response()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await using (await bus.Rpc.RespondAsync<Request, Response>(x =>
               {
                   return x switch
                   {
                       BunnyRequest b => new BunnyResponse(b.Id),
                       RabbitRequest r => new RabbitResponse(r.Id),
                       _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
                   };
               }, cts.Token))
        {
            var bunnyResponse = await bus.Rpc.RequestAsync<Request, Response>(
                new BunnyRequest(42), cts.Token
            );
            bunnyResponse.Should().Be(new BunnyResponse(42));

            var rabbitResponse = await bus.Rpc.RequestAsync<Request, Response>(
                new RabbitRequest(42), cts.Token
            );
            rabbitResponse.Should().Be(new RabbitResponse(42));
        }
    }
}
