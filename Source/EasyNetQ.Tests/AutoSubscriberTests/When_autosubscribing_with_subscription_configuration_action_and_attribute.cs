// ReSharper disable InconsistentNaming
using EasyNetQ.AutoSubscribe;
using EasyNetQ.Internals;
using FluentAssertions;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace EasyNetQ.Tests.AutoSubscriberTests
{
    public class When_autosubscribing_with_subscription_configuration_action_and_attribute : IDisposable
    {
        private IBus bus;
        private Action<ISubscriptionConfiguration> capturedAction;
        private IPubSub pubSub;

        public When_autosubscribing_with_subscription_configuration_action_and_attribute()
        {
            pubSub = Substitute.For<IPubSub>();
            bus = Substitute.For<IBus>();
            bus.PubSub.Returns(pubSub);

            var autoSubscriber = new AutoSubscriber(bus, "my_app")
                {
                        ConfigureSubscriptionConfiguration =
                                c => c.WithAutoDelete(false)
                                    .WithExpires(11)
                                    .WithPrefetchCount(11)
                                    .WithPriority(11)
                };

            pubSub.SubscribeAsync(
                    Arg.Is("MyActionAndAttributeTest"),
                    Arg.Any<Func<MessageA, CancellationToken, Task>>(),
                    Arg.Any<Action<ISubscriptionConfiguration>>()
                )
                .Returns(Task.FromResult(Substitute.For<ISubscriptionResult>()).ToAwaitableDisposable())
                .AndDoes(a => capturedAction = (Action<ISubscriptionConfiguration>)a.Args()[2]);

            autoSubscriber.Subscribe(new[] { typeof(MyConsumerWithActionAndAttribute) });
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
        public void Should_have_called_subscribe_with_attribute_values_notaction_values()
        {
            var subscriptionConfiguration = new SubscriptionConfiguration(1);

            capturedAction(subscriptionConfiguration);

            subscriptionConfiguration.AutoDelete.Should().BeTrue();
            subscriptionConfiguration.Expires.Should().Be(10);
            subscriptionConfiguration.PrefetchCount.Should().Be(10);
            subscriptionConfiguration.Priority.Should().Be(10);
        }

        // Discovered by reflection over test assembly, do not remove.
        private class MyConsumerWithActionAndAttribute : IConsume<MessageA>
        {
            [AutoSubscriberConsumer(SubscriptionId = "MyActionAndAttributeTest")]
            [SubscriptionConfiguration(AutoDelete = true, Expires = 10, PrefetchCount = 10, Priority = 10)]
            public void Consume(MessageA message, CancellationToken cancellationToken)
            {
            }
        }

        private class MessageA
        {
        }
    }
}

// ReSharper restore InconsistentNaming
