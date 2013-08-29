// ReSharper disable InconsistentNaming

#pragma warning disable 67 // disable event is never used warning

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

            interceptedSubscriptions.Count.ShouldEqual(4);
            interceptedSubscriptions.TrueForAll(i => i.Item2.Method.DeclaringType == typeof(DefaultMessageDispatcher)).ShouldBeTrue();

            CheckSubscriptionsContains<MessageA>(interceptedSubscriptions, "MyAppPrefix:e8afeaac27aeba31a42dea8e4d05308e");
            CheckSubscriptionsContains<MessageB>(interceptedSubscriptions, "MyExplicitId");
            CheckSubscriptionsContains<MessageC>(interceptedSubscriptions, "MyAppPrefix:cf5f54ed13478763e2da2bb3c9487baa");

            var messageADispatcher = (Action<MessageA>)interceptedSubscriptions.Single(x => x.Item2.GetType().GetGenericArguments()[0] == typeof(MessageA)).Item2;
            var message = new MessageA{ Text = "Hello World" };
            messageADispatcher(message);
        }

        /// <summary>
        /// We don't care about the order that consumers are discovered by reflection, just that
        /// they are discovered. This makes these tests less brittle.
        /// </summary>
        /// <typeparam name="MessageType"></typeparam>
        /// <param name="subscriptions"></param>
        /// <param name="subscriptionId"></param>
        private void CheckSubscriptionsContains<MessageType>(IEnumerable<Tuple<string, Delegate>> subscriptions, string subscriptionId)
        {
            var contains = subscriptions.Any(x =>
                x.Item1 == subscriptionId && x.Item2.Method.GetParameters()[0].ParameterType == typeof(MessageType)
                );

            contains.ShouldBeTrue(string.Format(
                "Subscription '{0}' of type {1} not found.", subscriptionId, typeof(MessageType).Name));
        }

        [Test]
        public void Should_be_able_to_take_control_of_subscription_id_generation()
        {
            var interceptedSubscriptions = new List<Tuple<string, Delegate>>();
            var busFake = new BusFake
            {
                InterceptSubscribe = (s, a) => interceptedSubscriptions.Add(new Tuple<string, Delegate>(s, a))
            };

            var autoSubscriber = new AutoSubscriber(busFake, "MyAppPrefix")
            {
                GenerateSubscriptionId = c => c.MessageType.Name.ToString(CultureInfo.InvariantCulture)
            };

            autoSubscriber.Subscribe(GetType().Assembly);

            interceptedSubscriptions.Count.ShouldEqual(4);

            CheckSubscriptionsContains<MessageA>(interceptedSubscriptions, "MessageA");
            CheckSubscriptionsContains<MessageB>(interceptedSubscriptions, "MyExplicitId");
            CheckSubscriptionsContains<MessageC>(interceptedSubscriptions, "MessageC");
        }

        [Test]
        public void Should_be_able_to_use_a_custom_message_dispatcher()
        {
            var interceptedSubscriptions = new List<Tuple<string, Delegate>>();
            var busFake = new BusFake
            {
                InterceptSubscribe = (s, a) => interceptedSubscriptions.Add(new Tuple<string, Delegate>(s, a))
            };

            var dispatcher = new CustomMessageDispatcher();

            var autoSubscriber = new AutoSubscriber(busFake, "MyAppPrefix")
            {
                MessageDispatcher = dispatcher
            };

            autoSubscriber.Subscribe(GetType().Assembly);

            var messageADispatcher = (Action<MessageA>)interceptedSubscriptions.Single(x => x.Item2.GetType().GetGenericArguments()[0] == typeof(MessageA)).Item2;
            var message = new MessageA();
            messageADispatcher(message);

            dispatcher.DispatchedMessage.ShouldBeTheSameAs(message);
        }

        // Discovered by reflection over test assembly, do not remove.
        private class MyConsumer : IConsume<MessageA>, IConsume<MessageB>, IConsume<MessageC>
        {
            public void Consume(MessageA message)
            {
                // Console.Out.WriteLine("Message handled: '{0}'", message.Text);
            }

            [Consumer(SubscriptionId = "MyExplicitId")]
            public void Consume(MessageB message) { }

            public void Consume(MessageC message) { }
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

            public IPublishChannel OpenPublishChannel(Action<IChannelConfiguration> configure)
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

        private class CustomMessageDispatcher : IMessageDispatcher
        {
            public object DispatchedMessage { get; private set; }

            public void Dispatch<TMessage, TConsumer>(TMessage message)
                where TMessage : class
                where TConsumer : IConsume<TMessage>
            {
                DispatchedMessage = message;
            }
        }
    }
}

// ReSharper restore InconsistentNaming