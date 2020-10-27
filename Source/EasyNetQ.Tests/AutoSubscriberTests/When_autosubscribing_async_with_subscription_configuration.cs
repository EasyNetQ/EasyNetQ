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
    public class When_autosubscribing_async_with_subscription_configuration_attribute : IDisposable
    {
        private readonly IBus bus;
        private Action<ISubscriptionConfiguration> capturedAction;
        private readonly IPubSub pubSub;

        public When_autosubscribing_async_with_subscription_configuration_attribute()
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
                .Returns(Task.FromResult(Substitute.For<ISubscriptionResult>()).ToAwaitableDisposable())
                .AndDoes(a => capturedAction = (Action<ISubscriptionConfiguration>)a.Args()[2]);

            autoSubscriber.Subscribe(new[] { typeof(MyConsumerWithAttr) });
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
        public void Should_have_called_subscribe_with_action_capable_of_configuring_subscription()
        {
            var subscriptionConfiguration = new SubscriptionConfiguration(1);

            capturedAction.Should().NotBeNull("SubscribeAsync should have been invoked");
            capturedAction(subscriptionConfiguration);

            subscriptionConfiguration.AutoDelete.Should().BeTrue();
            subscriptionConfiguration.Expires.Should().Be(10);
            subscriptionConfiguration.PrefetchCount.Should().Be((ushort)10);
            subscriptionConfiguration.Priority.Should().Be(10);
        }

        // Discovered by reflection over test assembly, do not remove.
        private class MyConsumerWithAttr : IConsumeAsync<MessageA>
        {
            [AutoSubscriberConsumer(SubscriptionId = "MyAttrTest")]
            [SubscriptionConfiguration(AutoDelete = true, Expires = 10, PrefetchCount = 10, Priority = 10)]
            public Task ConsumeAsync(MessageA message, CancellationToken cancellationToken)
            {
                return Task.FromResult(0);
            }
        }

        private class MessageA
        {
        }
    }

    public class When_autosubscribing_async_explicit_implementation_with_subscription_configuration_attribute : IDisposable
    {
        private readonly IBus bus;
        private Action<ISubscriptionConfiguration> capturedAction;
        private readonly IPubSub pubSub;

        public When_autosubscribing_async_explicit_implementation_with_subscription_configuration_attribute()
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
                   .Returns(Task.FromResult(Substitute.For<ISubscriptionResult>()).ToAwaitableDisposable())
                   .AndDoes(a => capturedAction = (Action<ISubscriptionConfiguration>)a.Args()[2]);

            autoSubscriber.Subscribe(new[] { typeof(MyConsumerWithAttr) });
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
        private class MyConsumerWithAttr : IConsumeAsync<MessageA>
        {
            [AutoSubscriberConsumer(SubscriptionId = "MyAttrTest")]
            [SubscriptionConfiguration(AutoDelete = true, Expires = 10, PrefetchCount = 10, Priority = 10)]
            Task IConsumeAsync<MessageA>.ConsumeAsync(MessageA message, CancellationToken cancellationToken)
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
