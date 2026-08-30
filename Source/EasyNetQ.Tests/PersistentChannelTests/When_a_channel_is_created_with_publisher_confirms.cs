using EasyNetQ.Persistent;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.PersistentChannelTests;

/// <summary>
///     Regression test: PersistentChannel must NOT enable the client's publisher-confirmation tracking.
///     With tracking enabled, RabbitMQ.Client 7.x awaits the broker confirm inside BasicPublishAsync, which
///     EasyNetQ invokes while holding the persistent channel mutex - serializing every confirmed publish on the
///     bus to one in flight (and duplicating EasyNetQ's own PublishConfirmationListener tracking and headers).
///     EasyNetQ enables confirms on the channel but tracks and awaits them itself, outside the mutex.
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
    public void Should_not_enable_the_clients_confirmation_tracking()
    {
        createChannelOptions.Should().NotBeNull();
        createChannelOptions.PublisherConfirmationTrackingEnabled.Should()
            .BeFalse("the client would await the confirm inside BasicPublishAsync while the persistent channel mutex is held, serializing confirmed publishes");
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
