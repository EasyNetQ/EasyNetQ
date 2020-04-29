using System;
using System.Collections.Generic;
using System.Text;
using EasyNetQ.Topology;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    public class TestingRabbitHutch
    {
        [Fact]
        public void CreateBus()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");
            bus.Advanced.ExchangeDeclare("cippa", ExchangeType.Topic);
        }
    }
}
