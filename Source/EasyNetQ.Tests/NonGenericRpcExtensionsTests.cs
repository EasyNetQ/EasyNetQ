namespace EasyNetQ.Tests;

public class NonGenericRpcExtensionsTests
{
    private readonly Action<IRequestConfiguration> configure = _ => { };
    private readonly IRpc rpc = Substitute.For<IRpc>();

    [Fact]
    public async Task Should_be_able_to_send_struct()
    {
        var request = DateTime.UtcNow;
        var requestType = typeof(DateTime);
        var responseType = typeof(long);
        rpc.RequestAsync<DateTime, long>(Arg.Any<DateTime>(), configure, cancellationToken: default).Returns(42);

        var response = await rpc.RequestAsync(request, requestType, responseType, configure, cancellationToken: default);
        response.Should().Be(42);

#pragma warning disable 4014
        rpc.Received()
            .RequestAsync<DateTime, long>(
                Arg.Is(request),
                Arg.Is(configure),
                Arg.Any<CancellationToken>()
            );
#pragma warning restore 4014
    }

    [Fact]
    public async Task Should_be_able_to_send()
    {
        var request = new Dog();
        var requestType = typeof(Dog);
        var responseType = typeof(string);
        rpc.RequestAsync<Dog, string>(Arg.Any<Dog>(), configure, cancellationToken: default).Returns("dog");

        var response = await rpc.RequestAsync(request, requestType, responseType, configure, cancellationToken: default);
        response.Should().Be("dog");

#pragma warning disable 4014
        rpc.Received()
            .RequestAsync<Dog, string>(
                Arg.Is(request),
                Arg.Is(configure),
                Arg.Any<CancellationToken>()
            );
#pragma warning restore 4014
    }

    [Fact]
    public async Task Should_be_able_to_publish_polymorphic()
    {
        var request = (IAnimal)new Dog();
        var requestType = typeof(IAnimal);
        var responseType = typeof(IAnimal);
        rpc.RequestAsync<IAnimal, IAnimal>(Arg.Any<IAnimal>(), configure, cancellationToken: default).Returns(request);

        var response = await rpc.RequestAsync(request, requestType, responseType, configure, cancellationToken: default);
        response.Should().Be(request);

#pragma warning disable 4014
        rpc.Received()
            .RequestAsync<IAnimal, IAnimal>(
                Arg.Is(request),
                Arg.Is(configure),
                Arg.Any<CancellationToken>()
            );
#pragma warning restore 4014
    }
}
