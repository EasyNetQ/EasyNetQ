using System.Text;

namespace EasyNetQ.IntegrationTests.Rpc;

[Collection("RabbitMQ")]
public class When_request_and_respond_with_publish_confirms : IDisposable
{
    public When_request_and_respond_with_publish_confirms(RabbitMQFixture fixture)
    {
        bus = RabbitHutch.CreateBus($"host={fixture.Host};prefetchCount=1;publisherConfirms=True;timeout=-1");
    }

    public void Dispose()
    {
        bus.Dispose();
    }

    private readonly SelfHostedBus bus;

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

    [Fact]
    public async Task Should_receive_header()
    {
        var headers = new Dictionary<string, object>()
        {
            { "test", "value" }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        
        using (await bus.Rpc.RespondAsync<Request, Response>((x, h, t) => Task.FromResult(new Response(x.Id, Encoding.UTF8.GetString((byte[])h.First().Value))), (config) => { }, cts.Token))
        {
            var response = await bus.Rpc.RequestAsync<Request, Response>(new Request(42), config => config.WithHeaders(headers), cts.Token);
            response.Should().Be(new Response(42, headers.Values.First().ToString()));
        }
    }
}
