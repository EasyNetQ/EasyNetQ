// ReSharper disable InconsistentNaming
using System;
using EasyNetQ.AutoSubscribe;
using EasyNetQ.FluentConfiguration;
using Xunit;
using NSubstitute;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;

namespace EasyNetQ.Tests.AutoSubscriberTests
{
    public class When_autosubscribing_async_with_subscription_configuration_action : IDisposable
    {
        private IBus bus;
        private Action<ISubscriptionConfiguration> capturedAction;
       
        public When_autosubscribing_async_with_subscription_configuration_action()
        {
            bus = Substitute.For<IBus>();
           

            var autoSubscriber = new AutoSubscriber(bus, "my_app")
                {
                        ConfigureSubscriptionConfiguration =
                                c => c.WithAutoDelete()
                                    .WithExpires(10)
                                    .WithPrefetchCount(10)
                                    .WithPriority(10)
                };

            bus.When(x => x.SubscribeAsync(
                    Arg.Is("MyActionTest"),
                    Arg.Any<Func<MessageA, Task>>(),
                    Arg.Any<Action<ISubscriptionConfiguration>>()
                    ))
                    .Do(a =>
                    {
                        capturedAction = (Action<ISubscriptionConfiguration>)a.Args()[2];
                    });

            autoSubscriber.SubscribeAsync(GetType().GetTypeInfo().Assembly);


        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
        public void Should_have_called_subscribe_async()
        {
            bus.Received().SubscribeAsync(Arg.Any<string>(),
                                     Arg.Any<Func<MessageA, Task>>(),
                                     Arg.Any<Action<ISubscriptionConfiguration>>());
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
        class MyConsumerWithAction : IConsumeAsync<MessageA>
        {
            [AutoSubscriberConsumer(SubscriptionId = "MyActionTest")]
            public Task ConsumeAsync(MessageA message)
            {
                return Task.FromResult(0);
            }
        }

        class MessageA
        {
            public string Text { get; set; }
        }
    }
}

// ReSharper restore InconsistentNaming