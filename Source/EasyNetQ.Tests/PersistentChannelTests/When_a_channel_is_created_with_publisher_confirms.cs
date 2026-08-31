using EasyNetQ.Persistent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.PersistentChannelTests;

/// <summary>
///     Confirmation tracking is delegated to RabbitMQ.Client: BasicPublishAsync completes when the broker
///     confirms. EasyNetQ starts the publish inside the channel mutex but awaits the in-flight task outside it
///     (StartConfirmedPublishAction), so confirmed publishes stay concurrent - bounded by the rate limiter,
///     which must be passed explicitly because the CreateChannelOptions ctor defaults it to null.
/// </summary>
public class When_a_channel_is_created_with_publisher_confirms : IAsyncLifetime
{
    private readonly IPersistentConnection persistentConnection;
    private readonly IPersistentChannel persistentChannel;
    private CreateChannelOptions createChannelOptions;

    public When_a_channel_is_created_with_publisher_confirms()
    {
        persistentConnection = Substitute.For<IPersistentConnection>();
        var channel = Substitute.For<IChannel, IRecoverable>();

#pragma warning disable IDISP004
        persistentConnection.CreateChannelAsync(Arg.Do<CreateChannelOptions>(x => createChannelOptions = x), default)
            .Returns(channel);
#pragma warning restore IDISP004

        persistentChannel = new PersistentChannel(
            new PersistentChannelOptions(publisherConfirms: true),
            Substitute.For<ILogger<PersistentChannel>>(),
            persistentConnection,
            Substitute.For<IEventBus>()
        );
    }

    public async ValueTask InitializeAsync()
    {
        await persistentChannel.InvokeChannelActionAsync(x => x.ExchangeDeclareAsync("MyExchange", ExchangeType.Direct));
    }

    public async ValueTask DisposeAsync()
    {
        await persistentChannel.DisposeAsync();
    }

    [Fact]
    public void Should_enable_publisher_confirms_on_the_channel()
    {
        createChannelOptions.Should().NotBeNull();
        createChannelOptions.PublisherConfirmationsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Should_enable_the_clients_confirmation_tracking_with_a_rate_limiter()
    {
        createChannelOptions.Should().NotBeNull();
        createChannelOptions.PublisherConfirmationTrackingEnabled.Should()
            .BeTrue("confirmation tracking is delegated to the client; the publish task is awaited outside the channel mutex");
        createChannelOptions.OutstandingPublisherConfirmationsRateLimiter.Should()
            .NotBeNull("the CreateChannelOptions ctor silently defaults the rate limiter to null");
    }
}

public class When_a_channel_is_created_without_publisher_confirms : IAsyncLifetime
{
    private readonly IPersistentConnection persistentConnection;
    private readonly IPersistentChannel persistentChannel;
    private CreateChannelOptions createChannelOptions;

    public When_a_channel_is_created_without_publisher_confirms()
    {
        persistentConnection = Substitute.For<IPersistentConnection>();
        var channel = Substitute.For<IChannel, IRecoverable>();

#pragma warning disable IDISP004
        persistentConnection.CreateChannelAsync(Arg.Do<CreateChannelOptions>(x => createChannelOptions = x), default)
            .Returns(channel);
#pragma warning restore IDISP004

        persistentChannel = new PersistentChannel(
            new PersistentChannelOptions(),
            Substitute.For<ILogger<PersistentChannel>>(),
            persistentConnection,
            Substitute.For<IEventBus>()
        );
    }

    public async ValueTask InitializeAsync()
    {
        await persistentChannel.InvokeChannelActionAsync(x => x.ExchangeDeclareAsync("MyExchange", ExchangeType.Direct));
    }

    public async ValueTask DisposeAsync()
    {
        await persistentChannel.DisposeAsync();
    }

    [Fact]
    public void Should_not_enable_publisher_confirms_or_tracking()
    {
        createChannelOptions.Should().NotBeNull();
        createChannelOptions.PublisherConfirmationsEnabled.Should().BeFalse();
        createChannelOptions.PublisherConfirmationTrackingEnabled.Should().BeFalse();
    }
}
