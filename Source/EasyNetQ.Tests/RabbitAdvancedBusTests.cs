using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;
using EasyNetQ.Producer;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests;

public class RabbitAdvancedBusTests
{
    [Theory]
    [InlineData(false, null, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, null, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task Should_use_mandatory_per_request_if_not_null_else_from_settings(bool mandatoryFromSettings, bool? mandatoryPerRequest, bool expected)
    {
        using var mockBuilder = new MockBuilder(
            x => x.AddSingleton(new ConnectionConfiguration { MandatoryPublish = mandatoryFromSettings })
        );

        await mockBuilder.Bus.Advanced.PublishAsync("", "", mandatoryPerRequest, null, new Message<object>(null));

        await mockBuilder.Channels[0].Received().BasicPublishAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<RabbitMQ.Client.BasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Is(expected),
            Arg.Any<CancellationToken>()
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
        var xxx = Substitute.For<IPublishConfirmationListener>();
        using var mockBuilder = new MockBuilder(
            x =>
            {
                x.AddSingleton(new ConnectionConfiguration { PublisherConfirms = confirmsFromSettings });
                x.AddSingleton(xxx);
            });

        await mockBuilder.Bus.Advanced.PublishAsync("", "", null, confirmsPerRequest, new Message<object>(null));

        xxx.Received(expected).CreatePendingConfirmation(Arg.Any<IChannel>());
        await mockBuilder.Channels[0].Received().BasicPublishAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<RabbitMQ.Client.BasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>()
        );
    }
}
