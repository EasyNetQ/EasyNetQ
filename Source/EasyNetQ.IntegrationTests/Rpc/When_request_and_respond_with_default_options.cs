using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.Rpc;

[Collection("RabbitMQ")]
public class When_request_and_respond_with_default_options : IAsyncLifetime
{
    private readonly RabbitMQFixture rmqFixture;

    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_request_and_respond_with_default_options(RabbitMQFixture rmqFixture)
    {
        this.rmqFixture = rmqFixture;
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};prefetchCount=1;timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (serviceProvider != null)
            await serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task Should_receive_exception()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await using (
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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

        await using var _ = await bus.Rpc.RespondAsync<Request, Response>(x => new Response(x.Id), cts.Token);

        {
            var response = await bus.Rpc.RequestAsync<Request, Response>(new Request(42), cts.Token);
            response.Should().Be(new Response(42));
        }

        await Task.Delay(TimeSpan.FromSeconds(10), cts.Token); // periodic consumer restart delay

        {
            var response = await bus.Rpc.RequestAsync<Request, Response>(new Request(42), cts.Token);
            response.Should().Be(new Response(42));
        }
    }

    [Fact]
    public async Task Should_survive_restart()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await using (await bus.Rpc.RespondAsync<Request, Response>(x => Task.FromResult(new Response(x.Id)), cts.Token))
        {
            await bus.Rpc.RequestAsync<Request, Response>(new Request(42), cts.Token);

            await rmqFixture.ManagementClient.KillAllConnectionsAsync(cts.Token);

            try
            {
                await bus.Rpc.RequestAsync<Request, Response>(new Request(42), cts.Token);
            }
            catch (Exception)
            {
                // The crunch to deal with the race when Rpc has not handled reconnection yet
            }

            await bus.Rpc.RequestAsync<Request, Response>(new Request(42), cts.Token);
        }
    }
}
