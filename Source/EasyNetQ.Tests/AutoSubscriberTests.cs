// ReSharper disable InconsistentNaming

#pragma warning disable 67 // disable event is never used warning

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class AutoSubscriberTests
    {
        [Test]
        public void Should_be_able_to_autosubscribe_to_several_messages_in_one_consumer()
        {
            var interceptedSubscriptions = new List<Tuple<string, Delegate>>();
            var busFake = new BusFake
            {
                InterceptSubscribe = (s, a) => interceptedSubscriptions.Add(new Tuple<string, Delegate>(s, a))
            };
            var autoSubscriber = new AutoSubscriber(busFake, "MyAppPrefix");

            autoSubscriber.Subscribe(GetType().Assembly);

            interceptedSubscriptions.Count.ShouldEqual(3);
            interceptedSubscriptions.TrueForAll(i => i.Item2.Method.DeclaringType == typeof(MyConsumer)).ShouldBeTrue();

            interceptedSubscriptions[0].Item1.ShouldEqual("MyAppPrefix:e8afeaac27aeba31a42dea8e4d05308e");
            interceptedSubscriptions[0].Item2.Method.GetParameters()[0].ParameterType.ShouldEqual(typeof(MessageA));

            interceptedSubscriptions[1].Item1.ShouldEqual("MyExplicitId");
            interceptedSubscriptions[1].Item2.Method.GetParameters()[0].ParameterType.ShouldEqual(typeof(MessageB));

            interceptedSubscriptions[2].Item1.ShouldEqual("MyAppPrefix:cf5f54ed13478763e2da2bb3c9487baa");
            interceptedSubscriptions[2].Item2.Method.GetParameters()[0].ParameterType.ShouldEqual(typeof(MessageC));
        }

        [Test]
        public void Should_be_able_to_autosubscribe_to_several_messages_in_one_consumer_with_custom_interface()
        {
            var interceptedSubscriptions = new List<Tuple<string, Delegate>>();
            var busFake = new BusFake
            {
                InterceptSubscribe = (s, a) => interceptedSubscriptions.Add(new Tuple<string, Delegate>(s, a))
            };
            var autoSubscriber = new AutoSubscriber(busFake, "MyAppPrefix");

            autoSubscriber.Subscribe(typeof(IConsumeCustom<>), GetType().Assembly);

            interceptedSubscriptions.Count.ShouldEqual(3);
            interceptedSubscriptions.TrueForAll(i => i.Item2.Method.DeclaringType == typeof(MyConsumerWithCustomInterface)).ShouldBeTrue();

            interceptedSubscriptions[0].Item1.ShouldEqual("MyAppPrefix:63c317b761366d57679a8bb0f7fa925a");
            interceptedSubscriptions[0].Item2.Method.GetParameters()[0].ParameterType.ShouldEqual(typeof(MessageA));

            interceptedSubscriptions[1].Item1.ShouldEqual("MyExplicitId");
            interceptedSubscriptions[1].Item2.Method.GetParameters()[0].ParameterType.ShouldEqual(typeof(MessageB));

            interceptedSubscriptions[2].Item1.ShouldEqual("MyAppPrefix:813fd8f08e61068e054dcff403da5ce7");
            interceptedSubscriptions[2].Item2.Method.GetParameters()[0].ParameterType.ShouldEqual(typeof(MessageC));
        }

        [Test]
        public void Should_be_able_to_take_control_of_subscription_id_generation()
        {
            var interceptedSubscriptions = new List<Tuple<string, Delegate>>();
            var busFake = new BusFake
            {
                InterceptSubscribe = (s, a) => interceptedSubscriptions.Add(new Tuple<string, Delegate>(s, a))
            };
            var fixedSubscriptionIds = new[] { "2f481170-8bc4-4d0f-a972-bd45191b1706", "be4ac633-ea29-4ed0-a0bf-419a01a0e9d4" };
            var callCount = 0;
            var autoSubscriber = new AutoSubscriber(busFake, "MyAppPrefix")
            {
                GenerateSubscriptionId = c => fixedSubscriptionIds[callCount++]
            };

            autoSubscriber.Subscribe(GetType().Assembly);

            interceptedSubscriptions.Count.ShouldEqual(3);
            interceptedSubscriptions[0].Item1.ShouldEqual(fixedSubscriptionIds[0]);
            interceptedSubscriptions[0].Item2.Method.GetParameters()[0].ParameterType.ShouldEqual(typeof(MessageA));

            interceptedSubscriptions[1].Item1.ShouldEqual("MyExplicitId");
            interceptedSubscriptions[1].Item2.Method.GetParameters()[0].ParameterType.ShouldEqual(typeof(MessageB));

            interceptedSubscriptions[2].Item1.ShouldEqual(fixedSubscriptionIds[1]);
            interceptedSubscriptions[2].Item2.Method.GetParameters()[0].ParameterType.ShouldEqual(typeof(MessageC));
        }

        // Discovered by reflection over test assembly, do not remove.
        private class MyConsumer : IConsume<MessageA>, IConsume<MessageB>, IConsume<MessageC>
        {
            public void Consume(MessageA message) { }

            [Consumer(SubscriptionId = "MyExplicitId")]
            public void Consume(MessageB message) { }

            public void Consume(MessageC message) { }
        }

        private class MyConsumerWithCustomInterface : IConsumeCustom<MessageA>, IConsumeCustom<MessageB>, IConsumeCustom<MessageC>
        {
            public void Consume(MessageA message) { }

            [Consumer(SubscriptionId = "MyExplicitId")]
            public void Consume(MessageB message) { }

            public void Consume(MessageC message) { }
        }

        private interface IConsumeCustom<in T>
        {
            void Consume(T message);
        }

        private class MessageA
        {
            public string Text { get; set; }
        }

        private class MessageB
        {
            public string Text { get; set; }
        }

        private class MessageC
        {
            public string Text { get; set; }
        }

        private class BusFake : IBus
        {
            public Action<string, Delegate> InterceptSubscribe;

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public IPublishChannel OpenPublishChannel()
            {
                throw new NotImplementedException();
            }

            public void Subscribe<T>(string subscriptionId, Action<T> onMessage)
            {
                if (InterceptSubscribe != null)
                    InterceptSubscribe(subscriptionId, onMessage);
            }

            public void Subscribe<T>(string subscriptionId, Action<T> onMessage, Action<ISubscriptionConfiguration<T>> configure)
            {
                throw new NotImplementedException();
            }

            public void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage)
            {
                throw new NotImplementedException();
            }

            public void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration<T>> configure)
            {
                throw new NotImplementedException();
            }

            public void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder)
            {
                throw new NotImplementedException();
            }

            public void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder, IDictionary<string, object> arguments)
            {
                throw new NotImplementedException();
            }

            public void RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
            {
                throw new NotImplementedException();
            }

            public void RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder, IDictionary<string, object> arguments)
            {
                throw new NotImplementedException();
            }

            public event Action Connected;
            public event Action Disconnected;
            public bool IsConnected { get; private set; }
            public IAdvancedBus Advanced { get; private set; }
        }
    }
}

// ReSharper restore InconsistentNaming