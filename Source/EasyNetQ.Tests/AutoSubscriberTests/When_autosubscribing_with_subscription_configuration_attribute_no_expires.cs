using System;
using EasyNetQ.AutoSubscribe;
using EasyNetQ.FluentConfiguration;
using NUnit.Framework;
using NSubstitute;
using System.Reflection;

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
            bus = Substitute.For<IBus>();
            
            var autoSubscriber = new AutoSubscriber(bus, "my_app");

            bus.When(x => x.Subscribe(Arg.Is("MyAttrTest"),
                                      Arg.Any<Action<MessageA>>(),
                                      Arg.Any<Action<ISubscriptionConfiguration>>()))
               .Do(a =>
               {
                   capturedAction = (Action<ISubscriptionConfiguration>)a.Args()[2];
               });

            autoSubscriber.Subscribe(GetType().GetTypeInfo().Assembly);
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test]
        public void Should_have_called_subscribe()
        {
            bus.Received().Subscribe(Arg.Any<string>(), 
                                     Arg.Any<Action<MessageA>>(),
                                     Arg.Any<Action<ISubscriptionConfiguration>>());
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