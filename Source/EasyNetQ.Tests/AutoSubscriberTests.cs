using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            const string fixedSubscriptionId = "2f481170-8bc4-4d0f-a972-bd45191b1706";
            var autoSubscriber = new AutoSubscriber(busFake)
            {
                SubscriptionIdFn = c => fixedSubscriptionId
            };

            autoSubscriber.Subscribe(GetType().Assembly);

            interceptedSubscriptions.Count.ShouldEqual(2);
            interceptedSubscriptions[0].Item1.ShouldEqual(fixedSubscriptionId);
            interceptedSubscriptions[0].Item2.Method.GetParameters()[0].ParameterType.ShouldEqual(typeof(MessageA));

            interceptedSubscriptions[1].Item1.ShouldEqual("MyExplicitId");
            interceptedSubscriptions[1].Item2.Method.GetParameters()[0].ParameterType.ShouldEqual(typeof(MessageB));
        }

        private class MyConsumer : IConsume<MessageA>, IConsume<MessageB>
        {
            public void Consume(MessageA message) { }

            [Consumer(SubscriptionId = "MyExplicitId")]
            public void Consume(MessageB message) { }
        }

        private class MessageA
        {
            public string Text { get; set; }
        }

        private class MessageB
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

            public void Subscribe<T>(string subscriptionId, Action<T> onMessage, IDictionary<string, object> arguments)
            {
                throw new NotImplementedException();
            }

            public void Subscribe<T>(string subscriptionId, string topic, Action<T> onMessage)
            {
                throw new NotImplementedException();
            }

            public void Subscribe<T>(string subscriptionId, string topic, Action<T> onMessage, IDictionary<string, object> arguments)
            {
                throw new NotImplementedException();
            }

            public void Subscribe<T>(string subscriptionId, IEnumerable<string> topics, Action<T> onMessage)
            {
                throw new NotImplementedException();
            }

            public void Subscribe<T>(string subscriptionId, IEnumerable<string> topics, Action<T> onMessage, IDictionary<string, object> arguments)
            {
                throw new NotImplementedException();
            }

            public void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage)
            {
                throw new NotImplementedException();
            }

            public void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, IDictionary<string, object> arguments)
            {
                throw new NotImplementedException();
            }

            public void SubscribeAsync<T>(string subscriptionId, string topic, Func<T, Task> onMessage)
            {
                throw new NotImplementedException();
            }

            public void SubscribeAsync<T>(string subscriptionId, string topic, Func<T, Task> onMessage, IDictionary<string, object> arguments)
            {
                throw new NotImplementedException();
            }

            public void SubscribeAsync<T>(string subscriptionId, IEnumerable<string> topics, Func<T, Task> onMessage)
            {
                throw new NotImplementedException();
            }

            public void SubscribeAsync<T>(string subscriptionId, IEnumerable<string> topics, Func<T, Task> onMessage, IDictionary<string, object> arguments)
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