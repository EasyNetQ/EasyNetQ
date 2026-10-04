using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EasyNetQ.Tests;

/// <summary>
///     The high level APIs must fall back to the connection level PublisherConfirms when a call does not
///     configure it explicitly, in the same way IAdvancedBus does for a null publisherConfirms argument.
/// </summary>
public class PublisherConfirmsConfigurationTests
{
    [Theory]
    [InlineData(false, null, 0)]
    [InlineData(false, false, 0)]
    [InlineData(false, true, 1)]
    [InlineData(true, null, 1)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 1)]
    public async Task PubSub_should_use_confirms_per_request_if_configured_else_from_settings(
        bool confirmsFromSettings, bool? confirmsPerRequest, int expected
    )
    {
        await using var mockBuilder = new MockBuilder(
            x => x.AddSingleton(new ConnectionConfiguration { PublisherConfirms = confirmsFromSettings })
        );

        await mockBuilder.Bus.PubSub.PublishAsync(
            new TestMessage(),
            x =>
            {
                if (confirmsPerRequest.HasValue)
                    x.WithPublisherConfirms(confirmsPerRequest.Value);
            },
            CancellationToken.None
        );

        // confirms are delegated to the client, so the publish channel's CreateChannelOptions reveal the choice
        await mockBuilder.Connection.Received(expected).CreateChannelAsync(
            Arg.Is<CreateChannelOptions>(o => o.PublisherConfirmationTrackingEnabled), Arg.Any<CancellationToken>()
        );
    }

    [Theory]
    [InlineData(false, null, 0)]
    [InlineData(false, false, 0)]
    [InlineData(false, true, 1)]
    [InlineData(true, null, 1)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 1)]
    public async Task Send_should_use_confirms_per_request_if_configured_else_from_settings(
        bool confirmsFromSettings, bool? confirmsPerRequest, int expected
    )
    {
        await using var mockBuilder = new MockBuilder(
            x => x.AddSingleton(new ConnectionConfiguration { PublisherConfirms = confirmsFromSettings })
        );

        await mockBuilder.Bus.SendReceive.SendAsync(
            "MyQueue",
            new TestMessage(),
            x =>
            {
                if (confirmsPerRequest.HasValue)
                    x.WithPublisherConfirms(confirmsPerRequest.Value);
            },
            CancellationToken.None
        );

        // confirms are delegated to the client, so the publish channel's CreateChannelOptions reveal the choice
        await mockBuilder.Connection.Received(expected).CreateChannelAsync(
            Arg.Is<CreateChannelOptions>(o => o.PublisherConfirmationTrackingEnabled), Arg.Any<CancellationToken>()
        );
    }

    [Theory]
    [InlineData(false, null, 0)]
    [InlineData(false, false, 0)]
    [InlineData(false, true, 1)]
    [InlineData(true, null, 1)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 1)]
    public async Task FuturePublish_should_use_confirms_per_request_if_configured_else_from_settings(
        bool confirmsFromSettings, bool? confirmsPerRequest, int expected
    )
    {
        await using var mockBuilder = new MockBuilder(
            x => x.AddSingleton(new ConnectionConfiguration { PublisherConfirms = confirmsFromSettings })
        );

        await mockBuilder.Bus.Scheduler.FuturePublishAsync(
            new TestMessage(),
            TimeSpan.FromMinutes(1),
            x =>
            {
                if (confirmsPerRequest.HasValue)
                    x.WithPublisherConfirms(confirmsPerRequest.Value);
            },
            CancellationToken.None
        );

        // confirms are delegated to the client, so the publish channel's CreateChannelOptions reveal the choice
        await mockBuilder.Connection.Received(expected).CreateChannelAsync(
            Arg.Is<CreateChannelOptions>(o => o.PublisherConfirmationTrackingEnabled), Arg.Any<CancellationToken>()
        );
    }

    [Theory]
    [InlineData(false, null, 0)]
    [InlineData(false, false, 0)]
    [InlineData(false, true, 1)]
    [InlineData(true, null, 1)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 1)]
    public async Task Request_should_use_confirms_per_request_if_configured_else_from_settings(
        bool confirmsFromSettings, bool? confirmsPerRequest, int expected
    )
    {
        var correlationId = Guid.NewGuid().ToString();
        await using var mockBuilder = new MockBuilder(
            x =>
            {
                x.AddSingleton(new ConnectionConfiguration { PublisherConfirms = confirmsFromSettings });
                x.AddSingleton<ICorrelationIdGenerationStrategy>(_ => new StaticCorrelationIdGenerationStrategy(correlationId));
            }
        );

        using var waiter = new CountdownEvent(2);
#pragma warning disable IDISP004
        mockBuilder.Published += () => waiter.Signal();
        mockBuilder.EventBus.Subscribe((StartConsumingSucceededEvent _) => Task.FromResult(waiter.Signal()));
#pragma warning restore IDISP004

        var request = mockBuilder.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(
            new TestRequestMessage(),
            x =>
            {
                if (confirmsPerRequest.HasValue)
                    x.WithPublisherConfirms(confirmsPerRequest.Value);
            },
            CancellationToken.None
        );
        if (!waiter.Wait(5000, TestContext.Current.CancellationToken))
            throw new TimeoutException();

        await mockBuilder.Consumers[0].HandleBasicDeliverAsync(
            "consumer_tag",
            0,
            false,
            "the_exchange",
            "the_routing_key",
            new BasicProperties
            {
                Type = "EasyNetQ.Tests.TestResponseMessage, EasyNetQ.Tests",
                CorrelationId = correlationId
            },
            "{ Text:\"Hello World\"}"u8.ToArray(),
            TestContext.Current.CancellationToken
        );
        await request;

        // confirms are delegated to the client, so the publish channel's CreateChannelOptions reveal the choice
        await mockBuilder.Connection.Received(expected).CreateChannelAsync(
            Arg.Is<CreateChannelOptions>(o => o.PublisherConfirmationTrackingEnabled), Arg.Any<CancellationToken>()
        );
    }
}
