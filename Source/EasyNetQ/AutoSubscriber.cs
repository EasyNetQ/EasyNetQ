using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EasyNetQ
{
    /// <summary>
    /// Lets you scan assemblies for implementations of <see cref="IConsume{T}"/> so that
    /// these will get registrered as subscribers in the bus.
    /// </summary>
    public class AutoSubscriber
    {
        protected readonly IBus Bus;

        /// <summary>
        /// Responsible for resolving a concrete subscriber. Defaults to
        /// parameterless constructor. This is where you hook in your IoC
        /// framework.
        /// </summary>
        public Func<Type, object> SubscriberFn { protected get; set; }

        /// <summary>
        /// Responsible for generating SubscriptionIds, when you use
        /// <see cref="IConsume{T}"/>, since it does not let you specify
        /// specific SubscriptionIds.
        /// Message type and SubscriptionId is the key; which if two
        /// equal keys exists, you will get round robin consumption of
        /// messages.
        /// </summary>
        public Func<ConsumerInfo, string> SubscriptionIdFn { protected get; set; }

        public AutoSubscriber(IBus bus)
        {
            if (bus == null)
                throw new ArgumentNullException("bus");

            Bus = bus;
            SubscriberFn = Activator.CreateInstance;
            SubscriptionIdFn = s => Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Registers all consumers in passed assembly. The actual Subscriber instances is
        /// created using <seealso cref="SubscriberFn"/>. The SubscriptionId per consumer
        /// method is determined by <seealso cref="SubscriptionIdFn"/> or if the method
        /// is marked with <see cref="ConsumerAttribute"/> with a custom SubscriptionId.
        /// </summary>
        /// <param name="assembly"></param>
        public virtual void Subscribe(Assembly assembly)
        {
            var genericBusSubscribeMethod = GetSubscribeMethodOfBus();
            var subscriptionInfos = GetSubscriptionInfos(assembly.GetTypes());

            foreach (var kv in subscriptionInfos)
            {
                var subscriber = SubscriberFn(kv.Key);
                foreach (var subscriptionInfo in kv.Value)
                {
                    var consumeMethod = GetConsumeMethodFor(subscriptionInfo);
                    var consumeAction = typeof(Action<>).MakeGenericType(subscriptionInfo.MessageType);
                    var consumeDelegate = Delegate.CreateDelegate(consumeAction, subscriber, consumeMethod);

                    var subscriptionAttribute = GetSubscriptionAttribute(consumeMethod);
                    var subscriptionId = subscriptionAttribute != null
                                             ? subscriptionAttribute.SubscriptionId
                                             : SubscriptionIdFn(subscriptionInfo);

                    var busSubscribeMethod = genericBusSubscribeMethod.MakeGenericMethod(subscriptionInfo.MessageType);
                    busSubscribeMethod.Invoke(Bus, new object[] { subscriptionId, consumeDelegate });
                }
            }
        }

        protected virtual MethodInfo GetSubscribeMethodOfBus()
        {
            return Bus.GetType().GetMethods()
                .Where(m => m.Name == "Subscribe")
                .Select(m => new { Method = m, Params = m.GetParameters() })
                .Single(m => m.Params.Length == 2
                    && m.Params[0].ParameterType == typeof(string)
                    && m.Params[1].ParameterType.GetGenericTypeDefinition() == typeof(Action<>)).Method;
        }

        protected virtual MethodInfo GetConsumeMethodFor(ConsumerInfo subscriptionInfo)
        {
            return subscriptionInfo.ConcreteType.GetMethod("Consume", new[] { subscriptionInfo.MessageType });
        }

        protected virtual ConsumerAttribute GetSubscriptionAttribute(MethodInfo consumeMethod)
        {
            return consumeMethod.GetCustomAttributes(typeof(ConsumerAttribute), true).SingleOrDefault() as ConsumerAttribute;
        }

        protected virtual IEnumerable<KeyValuePair<Type, ConsumerInfo[]>> GetSubscriptionInfos(IEnumerable<Type> types)
        {
            var marker = typeof(IConsume<>);

            foreach (var concreteType in types.Where(t => t.IsClass))
            {
                var subscriptionInfos = concreteType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == marker)
                    .Select(i => new ConsumerInfo(concreteType, i, i.GetGenericArguments()[0]))
                    .ToArray();

                if (subscriptionInfos.Any())
                    yield return new KeyValuePair<Type, ConsumerInfo[]>(concreteType, subscriptionInfos);
            }
        }
    }
}