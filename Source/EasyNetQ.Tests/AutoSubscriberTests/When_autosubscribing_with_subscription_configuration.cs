// ReSharper disable InconsistentNaming
using System;
using EasyNetQ.AutoSubscribe;
using EasyNetQ.FluentConfiguration;
using Xunit;
using NSubstitute;
using System.Reflection;
using FluentAssertions;

namespace EasyNetQ.Tests.AutoSubscriberTests
{
    public class When_autosubscribing_with_subscription_configuration_attribute : IDisposable
    {
        private IBus bus;
        private Action<ISubscriptionConfiguration> capturedAction;
       
        public When_autosubscribing_with_subscription_configuration_attribute()
        {
            bus = Substitute.For<IBus>();
            
            var autoSubscriber = new AutoSubscriber(bus, "my_app");

            bus.When(x => x.Subscribe(
                    Arg.Is("MyAttrTest"),
                    Arg.Any<Action<MessageA>>(),
                    Arg.Any<Action<ISubscriptionConfiguration>>()
                    ))
                    .Do(a =>
                    {
                        capturedAction = (Action<ISubscriptionConfiguration>)a.Args()[2];
                    });

            autoSubscriber.Subscribe(GetType().GetTypeInfo().Assembly);
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
        public void Should_have_called_subscribe()
        {
            bus.Received().Subscribe(
                        Arg.Any<string>(),
                        Arg.Any<Action<MessageA>>(),
                        Arg.Any<Action<ISubscriptionConfiguration>>()
                        );
        }

        [Fact]
        public void Should_have_called_subscribe_with_action_capable_of_configuring_subscription()
        {
            var subscriptionConfiguration = new SubscriptionConfiguration(1);
            
            capturedAction(subscriptionConfiguration);

            subscriptionConfiguration.AutoDelete.Should().BeTrue();
            subscriptionConfiguration.Expires.Should().Be(10);
            subscriptionConfiguration.PrefetchCount.Should().Be((ushort)10);
            subscriptionConfiguration.Priority.Should().Be(10);
        }

        // Discovered by reflection over test assembly, do not remove.
        private class MyConsumerWithAttr : IConsume<MessageA>
        {
            [AutoSubscriberConsumer(SubscriptionId = "MyAttrTest")]
            [SubscriptionConfiguration(AutoDelete = true, Expires = 10, PrefetchCount = 10, Priority = 10)]
            public void Consume(MessageA message)
            {
            }
        }

        private class MessageA
        {
            public string Text { get; set; }
        }
    }
}

// ReSharper restore InconsistentNaming