// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ConsumeTests
{
    [TestFixture]
    public class When_consume_is_called : ConsumerTestBase
    {
        protected override void AdditionalSetUp()
        {
            StartConsumer((body, properties, info) => { });
        }

        [Test]
        public void Should_create_a_consumer()
        {
            MockBuilder.Consumers.Count.ShouldEqual(1);
        }

        [Test]
        public void Should_create_a_channel_to_consume_on()
        {
            MockBuilder.Channels.Count.ShouldEqual(1);
        }

        [Test]
        public void Should_invoke_basic_consume_on_channel()
        {
            MockBuilder.Channels[0].AssertWasCalled(x => x.BasicConsume(
                Arg<string>.Is.Equal("my_queue"),
                Arg<bool>.Is.Equal(false), // NoAck
                Arg<string>.Is.Equal(ConsumerTag),
                Arg<bool>.Is.Equal(true),
                Arg<bool>.Is.Equal(false), 
                Arg<IDictionary<string, object>>.Is.Equal(new Dictionary<string, object>
                    {
                        {"x-priority", 0},
                        {"x-cancel-on-ha-failover", false}
                    }),
                Arg<IBasicConsumer>.Is.Same(MockBuilder.Consumers[0])));
        }

        [Test]
        public void Should_write_debug_message()
        {
            MockBuilder.Logger.AssertWasCalled(x =>
                                               x.InfoWrite(
                                                   "Declared Consumer. queue='{0}', consumer tag='{1}' prefetchcount={2} priority={3} x-cancel-on-ha-failover={4}",
                                                   "my_queue",
                                                   ConsumerTag,
                                                   (ushort) 50, 
                                                   0,
                                                   false));
        }
    }
}