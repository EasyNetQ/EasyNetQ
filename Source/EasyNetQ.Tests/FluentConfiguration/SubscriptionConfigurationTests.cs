// ReSharper disable InconsistentNaming

using System;
using EasyNetQ.FluentConfiguration;
using NUnit.Framework;

namespace EasyNetQ.Tests.FluentConfiguration
{
    [TestFixture]
    public class SubscriptionConfigurationTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void Should_set_properties_on_SubscriptionConfiguration()
        {
            Action<ISubscriptionConfiguration<object>> configure = x =>
                x
                    .WithArgument("key1", "value1")
                    .WithArgument("key2", "value2")
                    .WithTopic("abc")
                    .WithTopic("def");

            var configuration = new SubscriptionConfiguration<object>();
            configure(configuration);

            configuration.Topics[0].ShouldEqual("abc");
            configuration.Topics[1].ShouldEqual("def");
            configuration.Arguments["key1"].ShouldEqual("value1");
            configuration.Arguments["key2"].ShouldEqual("value2");
        }
    }
}

// ReSharper restore InconsistentNaming