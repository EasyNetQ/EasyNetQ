using System;
using EasyNetQ.AutoSubscribe;
using EasyNetQ.FluentConfiguration;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.AutoSubscriberTests
{
    [TestFixture]
    public class When_autosubscribing_with_subscription_configuration_attribute_no_expires
    {
        private IBus bus;
        private Action<ISubscriptionConfiguration> capturedAction;
       
        [SetUp]
        public void SetUp()
        {
            bus = MockRepository.GenerateMock<IBus>();
            
            var autoSubscriber = new AutoSubscriber(bus, "my_app");

            bus.Stub(x => x.Subscribe(
                Arg<string>.Is.Equal("MyAttrTest"),
                Arg<Action<MessageA>>.Is.Anything,
                Arg<Action<ISubscriptionConfiguration>>.Is.Anything
                ))
                .WhenCalled(a =>
                {
                    capturedAction= (Action<ISubscriptionConfiguration>)a.Arguments[2];
                });

            autoSubscriber.Subscribe(GetType().Assembly);
        }

        [Test]
        public void Should_have_called_subscribe()
        {
            bus.AssertWasCalled(
                x => x.Subscribe(
                    Arg<string>.Is.Anything, 
                    Arg<Action<MessageA>>.Is.Anything, 
                    Arg<Action<ISubscriptionConfiguration>>.Is.Anything));

        }

        [Test]
        public void Should_have_called_subscribe_with_no_expires()
        {
            var subscriptionConfiguration = new SubscriptionConfiguration(1);
            
            capturedAction(subscriptionConfiguration);

            subscriptionConfiguration.AutoDelete.ShouldBeTrue();
            subscriptionConfiguration.CancelOnHaFailover.ShouldBeTrue();
            subscriptionConfiguration.Expires.ShouldEqual(null);
            subscriptionConfiguration.PrefetchCount.ShouldEqual(10);
            subscriptionConfiguration.Priority.ShouldEqual(10);

        }

        // Discovered by reflection over test assembly, do not remove.
        private class MyConsumerWithAttr : IConsume<MessageA>
        {
            [AutoSubscriberConsumer(SubscriptionId = "MyAttrTest")]
            [SubscriptionConfiguration(AutoDelete = true, CancelOnHaFailover = true, PrefetchCount = 10, Priority = 10)]
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