using EasyNetQ.Topology;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    public class TestingRabbitHutch
    {
        [Fact]
        public void CreateBus()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");
            bus.Advanced.ExchangeDeclare("cippa", ExchangeType.Topic).Name.Should().Be("cippa");
        }
    }
}
