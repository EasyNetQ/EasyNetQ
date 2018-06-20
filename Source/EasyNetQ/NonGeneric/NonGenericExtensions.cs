using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ.NonGeneric
{
    public static class NonGenericExtensions
    {
        public static ISubscriptionResult Subscribe(this IBus bus, Type messageType, string subscriptionId, Action<object> onMessage)
        {
            return Subscribe(bus, messageType, subscriptionId, onMessage, configuration => { });
        }

        public static ISubscriptionResult Subscribe(
            this IBus bus,
            Type messageType,
            string subscriptionId,
            Action<object> onMessage,
            Action<ISubscriptionConfiguration> configure)
        {
            Preconditions.CheckNotNull(onMessage, "onMessage");

            Func<object, Task> asyncOnMessage = x => TaskHelpers.ExecuteSynchronously(() => onMessage(x));

            return SubscribeAsync(bus, messageType, subscriptionId, asyncOnMessage, configure);
        }

        public static ISubscriptionResult SubscribeAsync(
            this IBus bus,
            Type messageType,
            string subscriptionId,
            Func<object, Task> onMessage)
        {
            return SubscribeAsync(bus, messageType, subscriptionId, onMessage, configuration => { });
        }

        public static ISubscriptionResult SubscribeAsync(
            this IBus bus,
            Type messageType,
            string subscriptionId,
            Func<object, Task> onMessage,
            Action<ISubscriptionConfiguration> configure)
        {
            Preconditions.CheckNotNull(bus, "bus");
            Preconditions.CheckNotNull(messageType, "messageType");
            Preconditions.CheckNotNull(subscriptionId, "subscriptionId");
            Preconditions.CheckNotNull(onMessage, "onMessage");
            Preconditions.CheckNotNull(configure, "configure");

            var subscribeMethodOpen = typeof(IBus)
                .GetMethods()
                .SingleOrDefault(x => x.Name == "SubscribeAsync" && HasCorrectParameters(x));

            if (subscribeMethodOpen == null)
            {
                throw new EasyNetQException("API change? SubscribeAsync method not found on IBus");
            }

            var subscribeMethod = subscribeMethodOpen.MakeGenericMethod(messageType);
            return (ISubscriptionResult)subscribeMethod.Invoke(bus, new object[] { subscriptionId, onMessage, configure });
        } 

        public static void Publish(this IBus bus, Type messageType, object message)
        {
            PublishAsync(bus, messageType, message).GetAwaiter().GetResult();
        }

        public static void Publish(this IBus bus, Type messageType, object message, string topic)
        {
            PublishAsync(bus, messageType, message, topic).GetAwaiter().GetResult();
        }

        public static Task PublishAsync(this IBus bus, Type messageType, object message)
        {
            var conventions = bus.Advanced.Container.Resolve<IConventions>();
            return PublishAsync(bus, messageType, message, conventions.TopicNamingConvention(messageType));
        }

        public static Task PublishAsync(this IBus bus, Type messageType, object message, string topic)
        {
            Preconditions.CheckNotNull(message, "message");
            Preconditions.CheckNotNull(topic, "topic");
            Preconditions.CheckNotNull(messageType, "messageType");
            Preconditions.CheckTypeMatches(messageType, message, "message", "message must be of type " + messageType);
            
            var advancedBus = bus.Advanced.Container.Resolve<IAdvancedBus>();
            var publishExchangeDeclareStrategy = bus.Advanced.Container.Resolve<IPublishExchangeDeclareStrategy>();
            var messageDeliveryModeStrategy = bus.Advanced.Container.Resolve<IMessageDeliveryModeStrategy>();
            
            var exchange = publishExchangeDeclareStrategy.DeclareExchange(messageType, ExchangeType.Topic);
            var easyNetQMessage = MessageFactory.CreateInstance(messageType, message);
            easyNetQMessage.Properties.DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(messageType);

            return advancedBus.PublishAsync(exchange, topic, false, easyNetQMessage);
        }

        private static bool HasCorrectParameters(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();

            return 
                (parameters.Length == 3) && 
                (parameters[0].ParameterType == typeof(string) &&
                parameters[1].ParameterType.Name == "Func`2") &&
                parameters[2].ParameterType == typeof(Action<ISubscriptionConfiguration>);
        }
    }
}