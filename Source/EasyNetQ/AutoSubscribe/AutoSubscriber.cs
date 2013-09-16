using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace EasyNetQ.AutoSubscribe
{
    /// <summary>
    /// Lets you scan assemblies for implementations of <see cref="IConsume{T}"/> so that
    /// these will get registrered as subscribers in the bus.
    /// </summary>
    public class AutoSubscriber
    {
        protected const string ConsumeMethodName = "Consume";
        protected const string DispatchMethodName = "Dispatch";
        protected readonly IBus bus;

        /// <summary>
        /// Used when generating the unique SubscriptionId checksum.
        /// </summary>
        public string SubscriptionIdPrefix { get; private set; }

        /// <summary>
        /// Responsible for consuming a message with the relevant message consumer.
        /// </summary>
        public IAutoSubscriberMessageDispatcher AutoSubscriberMessageDispatcher { get; set; } 

        /// <summary>
        /// Responsible for generating SubscriptionIds, when you use
        /// <see cref="IConsume{T}"/>, since it does not let you specify
        /// specific SubscriptionIds.
        /// Message type and SubscriptionId is the key; which if two
        /// equal keys exists, you will get round robin consumption of
        /// messages.
        /// </summary>
        public Func<AutoSubscriberConsumerInfo, string> GenerateSubscriptionId { protected get; set; }

        public AutoSubscriber(IBus bus, string subscriptionIdPrefix)
        {
            Preconditions.CheckNotNull(bus, "bus");
            Preconditions.CheckNotBlank(subscriptionIdPrefix, "subscriptionIdPrefix", "You need to specify a SubscriptionId prefix, which will be used as part of the checksum of all generated subscription ids.");

            this.bus = bus;
            SubscriptionIdPrefix = subscriptionIdPrefix;
            AutoSubscriberMessageDispatcher = new DefaultAutoSubscriberMessageDispatcher();
            GenerateSubscriptionId = DefaultSubscriptionIdGenerator;
        }

        protected virtual string DefaultSubscriptionIdGenerator(AutoSubscriberConsumerInfo c)
        {
            var r = new StringBuilder();
            var unique = string.Concat(SubscriptionIdPrefix, ":", c.ConcreteType.FullName, ":", c.MessageType.FullName);

            using (var md5 = MD5.Create())
            {
                var buff = md5.ComputeHash(Encoding.UTF8.GetBytes(unique));
                foreach (var b in buff)
                    r.Append(b.ToString("x2"));
            }

            return string.Concat(SubscriptionIdPrefix, ":", r.ToString());
        }

        /// <summary>
        /// Registers all consumers in passed assembly. The actual Subscriber instances is
        /// created using <seealso cref="CreateConsumer"/>. The SubscriptionId per consumer
        /// method is determined by <seealso cref="GenerateSubscriptionId"/> or if the method
        /// is marked with <see cref="AutoSubscriberConsumerAttribute"/> with a custom SubscriptionId.
        /// </summary>
        /// <param name="assemblies">The assembleis to scan for consumers.</param>
        public virtual void Subscribe(params Assembly[] assemblies)
        {
            Preconditions.CheckAny(assemblies, "assemblies", "No assemblies specified.");

            var genericBusSubscribeMethod = GetSubscribeMethodOfBus();
            var subscriptionInfos = GetSubscriptionInfos(assemblies.SelectMany(a => a.GetTypes()));

            foreach (var kv in subscriptionInfos)
            {
                foreach (var subscriptionInfo in kv.Value)
                {
                    var dispatchMethod = AutoSubscriberMessageDispatcher.GetType()
                        .GetMethod(DispatchMethodName, BindingFlags.Instance | BindingFlags.Public)
                        .MakeGenericMethod(subscriptionInfo.MessageType, subscriptionInfo.ConcreteType);

                    var dispatchMethodType = typeof(Action<>).MakeGenericType(subscriptionInfo.MessageType);
                    var dispatchDelegate = Delegate.CreateDelegate(dispatchMethodType, AutoSubscriberMessageDispatcher, dispatchMethod);
                    var subscriptionAttribute = GetSubscriptionAttribute(subscriptionInfo);
                    var subscriptionId = subscriptionAttribute != null
                                             ? subscriptionAttribute.SubscriptionId
                                             : GenerateSubscriptionId(subscriptionInfo);

                    var busSubscribeMethod = genericBusSubscribeMethod.MakeGenericMethod(subscriptionInfo.MessageType);
                    busSubscribeMethod.Invoke(bus, new object[] { subscriptionId, dispatchDelegate });
                }
            }
        }

        protected virtual bool IsValidMarkerType(Type markerType)
        {
            return markerType.IsInterface && markerType.GetMethods().Any(m => m.Name == ConsumeMethodName);
        }

        protected virtual MethodInfo GetSubscribeMethodOfBus()
        {
            return bus.GetType().GetMethods()
                .Where(m => m.Name == "Subscribe")
                .Select(m => new { Method = m, Params = m.GetParameters() })
                .Single(m => m.Params.Length == 2
                    && m.Params[0].ParameterType == typeof(string)
                    && m.Params[1].ParameterType.GetGenericTypeDefinition() == typeof(Action<>)).Method;
        }
        
        protected virtual AutoSubscriberConsumerAttribute GetSubscriptionAttribute(AutoSubscriberConsumerInfo consumerInfo)
        {
            var consumeMethod = consumerInfo.ConcreteType.GetMethod(ConsumeMethodName, new[] { consumerInfo.MessageType });

            return consumeMethod.GetCustomAttributes(typeof(AutoSubscriberConsumerAttribute), true).SingleOrDefault() as AutoSubscriberConsumerAttribute;
        }

        protected virtual IEnumerable<KeyValuePair<Type, AutoSubscriberConsumerInfo[]>> GetSubscriptionInfos(IEnumerable<Type> types)
        {
            foreach (var concreteType in types.Where(t => t.IsClass))
            {
                var subscriptionInfos = concreteType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsume<>))
                    .Select(i => new AutoSubscriberConsumerInfo(concreteType, i, i.GetGenericArguments()[0]))
                    .ToArray();

                if (subscriptionInfos.Any())
                    yield return new KeyValuePair<Type, AutoSubscriberConsumerInfo[]>(concreteType, subscriptionInfos);
            }
        }
    }
}