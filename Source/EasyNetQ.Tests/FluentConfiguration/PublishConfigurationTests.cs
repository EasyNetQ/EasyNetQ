using System;
using Xunit;

namespace EasyNetQ.Tests.FluentConfiguration
{
    public class PublishConfigurationTests
    {
        [Fact]
        public void Should_throw_if_default_topic_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new PublishConfiguration(null));
        }

        [Fact]
        public void Should_throw_if_customer_topic_is_null()
        {
            var configuration = new PublishConfiguration("default");
            Assert.Throws<ArgumentNullException>(() => configuration.WithTopic(null));
        }

        [Fact]
        public void Should_return_default_topic()
        {
            var configuration = new PublishConfiguration("default");
            Assert.Equal("default", configuration.Topic);
        }

        [Fact]
        public void Should_return_custom_topic()
        {
            var configuration = new PublishConfiguration("default");

            configuration.WithTopic("custom");

            Assert.Equal("custom", configuration.Topic);
        }
    }
}
