using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;
using EasyNetQ.Producer;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests;

public class RabbitAdvancedBusTests
{
    private const string correlationId = "abc123";

    [Theory]
    [InlineData(false, null, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, null, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task Should_use_mandatory_per_request_if_not_null_else_from_settings(bool mandatoryFromSettings, bool? mandatoryPerRequest, bool expected)
    {
        await using var mockBuilder = new MockBuilder(
            x => x.AddSingleton(new ConnectionConfiguration { MandatoryPublish = mandatoryFromSettings })
                .AddSingleton<ICorrelationIdGenerationStrategy>(new StaticCorrelationIdGenerationStrategy(correlationId))
        );

        await mockBuilder.Bus.Advanced.PublishAsync("", "", mandatoryPerRequest, null, new Message<object?>(null), cancellationToken: CancellationToken.None);

        mockBuilder.PublishedOn(mockBuilder.Channels[0]).Should().ContainSingle().Which.Should().BeEquivalentTo(
            new PublishedMessage("", "", expected, new RabbitMQ.Client.BasicProperties { CorrelationId = correlationId, Type = "System.Object, System.Private.CoreLib" }, [])
        );
    }

    [Theory]
    [InlineData(false, null, 0)]
    [InlineData(false, false, 0)]
    [InlineData(false, true, 1)]
    [InlineData(true, null, 1)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 1)]
    public async Task Should_use_confirms_per_request_if_not_null_else_from_settings(bool confirmsFromSettings, bool? confirmsPerRequest, int expected)
    {
        await using var mockBuilder = new MockBuilder(
            x => x.AddSingleton(new ConnectionConfiguration { PublisherConfirms = confirmsFromSettings })
                .AddSingleton<ICorrelationIdGenerationStrategy>(new StaticCorrelationIdGenerationStrategy(correlationId)));

        await mockBuilder.Bus.Advanced.PublishAsync("", "", null, confirmsPerRequest, new Message<object?>(null), cancellationToken: CancellationToken.None);

        // confirms are delegated to the client, so the publish channel's CreateChannelOptions reveal the choice
        await mockBuilder.Connection.Received(expected).CreateChannelAsync(
            Arg.Is<RabbitMQ.Client.CreateChannelOptions>(o => o.PublisherConfirmationTrackingEnabled), Arg.Any<CancellationToken>());
        mockBuilder.PublishedOn(mockBuilder.Channels[0]).Should().ContainSingle().Which.Should().BeEquivalentTo(
            new PublishedMessage("", "", false, new RabbitMQ.Client.BasicProperties { CorrelationId = correlationId, Type = "System.Object, System.Private.CoreLib" }, [])
        );
    }
}
