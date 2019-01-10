using System;
using EasyNetQ.AutoSubscribe;
using Xunit;
using NSubstitute;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using FluentAssertions;

namespace EasyNetQ.Tests.AutoSubscriberTests
{
    public class When_autosubscribing_with_subscription_configuration_attribute_no_expires : IDisposable
    {
        private IBus bus;
        private Action<ISubscriptionConfiguration> capturedAction;
        private IPubSub pubSub;

        public When_autosubscribing_with_subscription_configuration_attribute_no_expires()
        {
            pubSub = Substitute.For<IPubSub>();
            bus = Substitute.For<IBus>();
            bus.PubSub.Returns(pubSub);
            
            var autoSubscriber = new AutoSubscriber(bus, "my_app");

            pubSub.SubscribeAsync(
                    Arg.Is("MyAttrTest"), 
                    Arg.Any<Func<MessageA, CancellationToken, Task>>(),
                    Arg.Any<Action<ISubscriptionConfiguration>>()
                )
                .Returns(TaskHelpers.FromResult(Substitute.For<ISubscriptionResult>()).ToAwaitableDisposable())
                .AndDoes(a => capturedAction = (Action<ISubscriptionConfiguration>)a.Args()[2]);

            autoSubscriber.Subscribe(new[] {typeof(MyConsumerWithAttr)});
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
        public void Should_have_called_subscribe()
        {
            pubSub.Received().SubscribeAsync(
                Arg.Any<string>(), 
                Arg.Any<Func<MessageA, CancellationToken, Task>>(),
                Arg.Any<Action<ISubscriptionConfiguration>>()
            );
        }

        [Fact]
        public void Should_have_called_subscribe_with_no_expires()
        {
            var subscriptionConfiguration = new SubscriptionConfiguration(1);
            
            capturedAction(subscriptionConfiguration);

            subscriptionConfiguration.AutoDelete.Should().BeTrue();
            subscriptionConfiguration.Expires.Should().Be(null);
            subscriptionConfiguration.PrefetchCount.Should().Be(10);
            subscriptionConfiguration.Priority.Should().Be(10);
        }

        // Discovered by reflection over test assembly, do not remove.
        private class MyConsumerWithAttr : IConsume<MessageA>
        {
            [AutoSubscriberConsumer(SubscriptionId = "MyAttrTest")]
            [SubscriptionConfiguration(AutoDelete = true, PrefetchCount = 10, Priority = 10)]
            public void Consume(MessageA message, CancellationToken cancellationToken)
            {
            }
        }

        private class MessageA
        {
        }
    }
}