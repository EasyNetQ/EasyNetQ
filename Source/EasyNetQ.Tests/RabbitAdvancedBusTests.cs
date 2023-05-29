using EasyNetQ.DI;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using RabbitMQ.Client;
using System.Text;

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

        await mockBuilder.Bus.Advanced.PublishAsync("", "", mandatoryPerRequest, new Message<object>(null));

        mockBuilder.Channels[0].Received().BasicPublish(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is(expected),
            Arg.Any<IBasicProperties>(),
            Arg.Any<ReadOnlyMemory<byte>>()
        );
    }
}
