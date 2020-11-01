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
    public class When_autosubscribing_async_with_subscription_configuration_action : IDisposable
    {
        private IBus bus;
        private Action<ISubscriptionConfiguration> capturedAction;
        private IPubSub pubSub;

        public When_autosubscribing_async_with_subscription_configuration_action()
        {
            pubSub = Substitute.For<IPubSub>();
            bus = Substitute.For<IBus>();
            bus.PubSub.Returns(pubSub);

            var autoSubscriber = new AutoSubscriber(bus, "my_app")
                {
                        ConfigureSubscriptionConfiguration =
                                c => c.WithAutoDelete()
                                    .WithExpires(10)
                                    .WithPrefetchCount(10)
                                    .WithPriority(10)
                };

            pubSub.SubscribeAsync(
                    Arg.Is("MyActionTest"),
                    Arg.Any<Func<MessageA, CancellationToken, Task>>(),
                    Arg.Any<Action<ISubscriptionConfiguration>>()
                )
                .Returns(Task.FromResult(Substitute.For<ISubscriptionResult>()).ToAwaitableDisposable())
                .AndDoes(a => capturedAction = (Action<ISubscriptionConfiguration>)a.Args()[2]);

            autoSubscriber.Subscribe(new[] { typeof(MyConsumerWithAction) });
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
        public void Should_have_called_subscribe_async()
        {
            pubSub.Received().SubscribeAsync(
                Arg.Any<string>(),
                Arg.Any<Func<MessageA, CancellationToken, Task>>(),
                Arg.Any<Action<ISubscriptionConfiguration>>()
            );
        }

        [Fact]
        public void Should_have_called_subscribe_with_action_capable_of_configuring_subscription()
        {
            var subscriptionConfiguration = new SubscriptionConfiguration(1);

            capturedAction.Should().NotBeNull("SubscribeAsync should have been invoked");

            capturedAction(subscriptionConfiguration);

            subscriptionConfiguration.AutoDelete.Should().BeTrue();
            subscriptionConfiguration.Expires.Should().Be(10);
            subscriptionConfiguration.PrefetchCount.Should().Be(10);
            subscriptionConfiguration.Priority.Should().Be(10);
        }

        // Discovered by reflection over test assembly, do not remove.
        // ReSharper disable once UnusedMember.Local
        private class MyConsumerWithAction : IConsumeAsync<MessageA>
        {
            [AutoSubscriberConsumer(SubscriptionId = "MyActionTest")]
            public Task ConsumeAsync(MessageA message, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        private class MessageA
        {
        }
    }
}

// ReSharper restore InconsistentNaming
