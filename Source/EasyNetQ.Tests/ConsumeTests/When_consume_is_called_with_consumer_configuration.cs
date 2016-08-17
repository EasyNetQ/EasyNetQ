// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using NUnit.Framework;
using RabbitMQ.Client;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ConsumeTests
{
    [TestFixture]
    public class When_consume_is_called_with_consumer_configuration : ConsumerTestBase
    {
        private string _consumerTag;

        protected override void AdditionalSetUp()
        {
            _consumerTag = DateTime.Now.Ticks.ToString();
            StartConsumer((body, properties, info) => { }, c => c.WithConsuemrTag(_consumerTag));
        }

        [Test]
        public void Should_invoke_basic_consume_on_channel_with_provided_consumer_tag()
        {
            MockBuilder.Channels[0].AssertWasCalled(x => x.BasicConsume(
                Arg<string>.Is.Anything,
                Arg<bool>.Is.Anything, // NoAck
                Arg<string>.Is.Equal(_consumerTag),
                Arg<bool>.Is.Anything,
                Arg<bool>.Is.Anything,
                Arg<IDictionary<string, object>>.Is.Anything,
                Arg<IBasicConsumer>.Is.Same(MockBuilder.Consumers[0])));
        }

        [Test]
        public void Should_have_convention_provided_consumer_tag_by_default()
        {
            string consumerTag = null;
            StartConsumer((body, properties, info) => { }, c => consumerTag = c.ConsumerTag);
            Assert.AreEqual(ConsumerTag, consumerTag);
        }
    }
}