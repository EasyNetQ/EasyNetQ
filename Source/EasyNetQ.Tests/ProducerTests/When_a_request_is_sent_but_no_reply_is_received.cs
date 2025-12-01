using EasyNetQ.Tests.Mocking;

namespace EasyNetQ.Tests.ProducerTests;

public class When_a_request_is_sent_but_no_reply_is_received : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder = new("host=localhost;timeout=1");

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public Task Should_throw_a_cancelled_exception()
    {
        return Assert.ThrowsAsync<TaskCanceledException>(
            () => mockBuilder.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(
                new TestRequestMessage(), _ => { }
            )
        );
    }
}
