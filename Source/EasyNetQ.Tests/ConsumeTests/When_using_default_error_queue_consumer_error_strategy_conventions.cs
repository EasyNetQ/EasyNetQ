// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_using_default_error_queue_consumer_error_strategy_conventions
{
    public When_using_default_error_queue_consumer_error_strategy_conventions()
    {
        conventions = new ErrorQueueConsumerErrorStrategyConventions();
    }

    private readonly ErrorQueueConsumerErrorStrategyConventions conventions;

    [Fact]
    public void The_default_error_exchange_name_should_be()
    {
        var info = new MessageReceivedInfo("consumer_tag", 0, false, "exchange", "routingKey", "queue");

        var result = conventions.ErrorExchangeNamingConvention(info);
        result.Should().Be("ErrorExchange_routingKey");
    }

    [Fact]
    public void The_default_error_queue_name_should_be()
    {
        var info = new MessageReceivedInfo("consumer_tag", 0, false, "exchange", "routingKey", "queue");
        var result = conventions.ErrorQueueNamingConvention(info);
        result.Should().Be("EasyNetQ_Default_Error_Queue");
    }
}
// ReSharper restore InconsistentNaming
