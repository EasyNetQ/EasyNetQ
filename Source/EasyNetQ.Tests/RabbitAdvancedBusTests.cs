using EasyNetQ.DI;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;
using System.Text;
using EasyNetQ.Producer;

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
        var mockBuilder = new MockBuilder(
            x => x.Register(new ConnectionConfiguration { MandatoryPublish = mandatoryFromSettings })
        );

        await mockBuilder.Bus.Advanced.PublishAsync("", "", mandatoryPerRequest, null, new Message<object>(null));

        mockBuilder.Channels[0].Received().BasicPublish(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is(expected),
            Arg.Any<IBasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>()
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
        var mockBuilder = new MockBuilder(
            x =>
            {
                x.Register(new ConnectionConfiguration { PublisherConfirms = confirmsFromSettings });
                x.Register(xxx);
            });

        await mockBuilder.Bus.Advanced.PublishAsync("", "", null, confirmsPerRequest, new Message<object>(null));

        xxx.Received(expected).CreatePendingConfirmation(Arg.Any<IModel>());
        mockBuilder.Channels[0].Received().BasicPublish(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<IBasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>()
        );
    }
}
