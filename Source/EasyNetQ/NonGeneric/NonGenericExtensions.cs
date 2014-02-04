using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;

namespace EasyNetQ.NonGeneric
{
    public static class NonGenericExtensions
    {
        #region Subscriptions
        public static IDisposable Subscribe(this IBus bus, Type messageType, string subscriptionId, Action<object> onMessage)
        {
            return Subscribe(bus, messageType, subscriptionId, onMessage, configuration => { });
        }

        public static IDisposable Subscribe(
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

        public static IDisposable SubscribeAsync(
            this IBus bus,
            Type messageType,
            string subscriptionId,
            Func<object, Task> onMessage)
        {
            return SubscribeAsync(bus, messageType, subscriptionId, onMessage, configuration => { });
        }

        public static IDisposable SubscribeAsync(
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
            return (IDisposable)subscribeMethod.Invoke(bus, new object[] { subscriptionId, onMessage, configure });
        } 
        #endregion

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