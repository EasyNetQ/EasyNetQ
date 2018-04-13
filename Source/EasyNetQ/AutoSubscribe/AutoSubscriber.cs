using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;
    
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
        protected const string DispatchAsyncMethodName = "DispatchAsync";
        protected readonly IBus bus;

        /// <summary>
        /// Used when generating the unique SubscriptionId checksum.
        /// </summary>
        public string SubscriptionIdPrefix { get; }

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

        /// <summary>
        /// Responsible for setting subscription configuration for all 
        /// auto subscribed consumers <see cref="IConsume{T}"/>.
        /// the values may be overriden for particular consumer 
        /// methods by using an <see cref="SubscriptionConfigurationAttribute"/>.
        /// </summary>
        public Action<ISubscriptionConfiguration> ConfigureSubscriptionConfiguration { protected get; set; }

        public AutoSubscriber(IBus bus, string subscriptionIdPrefix)
        {
            Preconditions.CheckNotNull(bus, "bus");
            Preconditions.CheckNotBlank(subscriptionIdPrefix, "subscriptionIdPrefix", "You need to specify a SubscriptionId prefix, which will be used as part of the checksum of all generated subscription ids.");

            this.bus = bus;
            SubscriptionIdPrefix = subscriptionIdPrefix;
            AutoSubscriberMessageDispatcher = new DefaultAutoSubscriberMessageDispatcher();
            GenerateSubscriptionId = DefaultSubscriptionIdGenerator;
            ConfigureSubscriptionConfiguration = subscriptionConfiguration => {};
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
        /// created using <seealso cref="AutoSubscriberMessageDispatcher"/>. The SubscriptionId per consumer
        /// method is determined by <seealso cref="GenerateSubscriptionId"/> or if the method
        /// is marked with <see cref="AutoSubscriberConsumerAttribute"/> with a custom SubscriptionId.
        /// </summary>
        /// <param name="assemblies">The assemblies to scan for consumers.</param>
        public virtual void Subscribe(params Assembly[] assemblies)
        {
            Preconditions.CheckAny(assemblies, "assemblies", "No assemblies specified.");

            Subscribe(assemblies.SelectMany(a => a.GetTypes()).ToArray());
        }

        /// <summary>
        /// Registers all types as consumers. The actual Subscriber instances is
        /// created using <seealso cref="AutoSubscriberMessageDispatcher"/>. The SubscriptionId per consumer
        /// method is determined by <seealso cref="GenerateSubscriptionId"/> or if the method
        /// is marked with <see cref="AutoSubscriberConsumerAttribute"/> with a custom SubscriptionId.
        /// </summary>
        /// <param name="consumerTypes">the types to register as consumers.</param>
        public virtual void Subscribe(params Type[] consumerTypes)
        {
            if (consumerTypes == null) throw new ArgumentNullException("consumerTypes");

            var genericBusSubscribeMethod = GetSubscribeMethodOfBus("Subscribe",typeof(Action<>));
            var subscriptionInfos = GetSubscriptionInfos(consumerTypes, typeof(IConsume<>));

            InvokeMethods(subscriptionInfos,DispatchMethodName, genericBusSubscribeMethod, messageType => typeof(Action<>).MakeGenericType(messageType));
        }

        /// <summary>
        /// Registers all async consumers in passed assembly. The actual Subscriber instances is
        /// created using <seealso cref="AutoSubscriberMessageDispatcher"/>. The SubscriptionId per consumer
        /// method is determined by <seealso cref="GenerateSubscriptionId"/> or if the method
        /// is marked with <see cref="AutoSubscriberConsumerAttribute"/> with a custom SubscriptionId.
        /// </summary>
        /// <param name="assemblies">The assemblies to scan for consumers.</param>
        public virtual void SubscribeAsync(params Assembly[] assemblies)
        {
            Preconditions.CheckAny(assemblies, "assemblies", "No assemblies specified.");

            SubscribeAsync(assemblies.SelectMany(a => a.GetTypes()).ToArray());
        }

        /// <summary>
        /// Registers all async consumers in passed assembly. The actual Subscriber instances is
        /// created using <seealso cref="AutoSubscriberMessageDispatcher"/>. The SubscriptionId per consumer
        /// method is determined by <seealso cref="GenerateSubscriptionId"/> or if the method
        /// is marked with <see cref="AutoSubscriberConsumerAttribute"/> with a custom SubscriptionId.
        /// </summary>
        /// <param name="consumerTypes">the types to register as consumers.</param>
        public virtual void SubscribeAsync(params Type[] consumerTypes)
        {
            var genericBusSubscribeMethod = GetSubscribeMethodOfBus("SubscribeAsync", typeof(Func<,>));
            var subscriptionInfos = GetSubscriptionInfos(consumerTypes, typeof(IConsumeAsync<>));
            Func<Type,Type> subscriberTypeFromMessageTypeDelegate = messageType => typeof (Func<,>).MakeGenericType(messageType, typeof (Task));
            InvokeMethods(subscriptionInfos, DispatchAsyncMethodName, genericBusSubscribeMethod, subscriberTypeFromMessageTypeDelegate);
        }

        protected virtual void InvokeMethods(IEnumerable<KeyValuePair<Type, AutoSubscriberConsumerInfo[]>> subscriptionInfos, string dispatchName, MethodInfo genericBusSubscribeMethod, Func<Type, Type> subscriberTypeFromMessageTypeDelegate)
        {
            foreach (var kv in subscriptionInfos)
            {
                foreach (var subscriptionInfo in kv.Value)
                {
                    var dispatchMethod =
                            AutoSubscriberMessageDispatcher.GetType()
                                                           .GetMethod(dispatchName, BindingFlags.Instance | BindingFlags.Public)
                                                           .MakeGenericMethod(subscriptionInfo.MessageType, subscriptionInfo.ConcreteType);

#if !NETFX
                    var dispatchDelegate = dispatchMethod.CreateDelegate(
                        subscriberTypeFromMessageTypeDelegate(subscriptionInfo.MessageType),
                        AutoSubscriberMessageDispatcher);
#else
                    var dispatchDelegate = Delegate.CreateDelegate(subscriberTypeFromMessageTypeDelegate(subscriptionInfo.MessageType), AutoSubscriberMessageDispatcher, dispatchMethod);
#endif

                    var subscriptionAttribute = GetSubscriptionAttribute(subscriptionInfo);
                    var subscriptionId = subscriptionAttribute != null ? subscriptionAttribute.SubscriptionId : GenerateSubscriptionId(subscriptionInfo);
                    var busSubscribeMethod = genericBusSubscribeMethod.MakeGenericMethod(subscriptionInfo.MessageType);
                    Action<ISubscriptionConfiguration> configAction = GenerateConfigurationAction(subscriptionInfo);
                    busSubscribeMethod.Invoke(bus, new object[] { subscriptionId, dispatchDelegate, configAction });
                }
            }
        }

        private Action<ISubscriptionConfiguration> GenerateConfigurationAction(AutoSubscriberConsumerInfo subscriptionInfo)
        {
            return sc =>
                {
                    ConfigureSubscriptionConfiguration(sc);
                    TopicInfo(subscriptionInfo)(sc);
                    AutoSubscriberConsumerInfo(subscriptionInfo)(sc);
                };

        }

        private Action<ISubscriptionConfiguration> TopicInfo(AutoSubscriberConsumerInfo subscriptionInfo)
        {
            var topics = GetTopAttributeValues(subscriptionInfo);
            if (topics.Count() != 0)
            {
                return GenerateConfigurationFromTopics(topics);
            }
            return configuration => configuration.WithTopic("#");
        }

        private Action<ISubscriptionConfiguration> GenerateConfigurationFromTopics(IEnumerable<string> topics)
        {
            return configuration =>
                {
                    foreach (var topic in topics)
                    {
                        configuration.WithTopic(topic);
                    }
                };
        }

        private IEnumerable<string> GetTopAttributeValues(AutoSubscriberConsumerInfo subscriptionInfo)
        {
            var consumeMethod = ConsumeMethod(subscriptionInfo);
            object[] customAttributes = consumeMethod.GetCustomAttributes(typeof(ForTopicAttribute), true).Cast<object>().ToArray();
            return customAttributes
                             .OfType<ForTopicAttribute>()
                             .Select(a => a.Topic);
        }

        private Action<ISubscriptionConfiguration> AutoSubscriberConsumerInfo(AutoSubscriberConsumerInfo subscriptionInfo)
        {
            var configSettings = GetSubscriptionConfigurationAttributeValue(subscriptionInfo);
            if(configSettings == null)
            {
                return subscriptionConfiguration => {};
            }
            return configuration =>
                {
                    //prefetch count is set to a configurable default in RabbitAdvancedBus
                    //so don't touch it unless SubscriptionConfigurationAttribute value is other than 0.
                    if (configSettings.PrefetchCount > 0)
                        configuration.WithPrefetchCount(configSettings.PrefetchCount);

                    if (configSettings.Expires > 0 )
                        configuration.WithExpires(configSettings.Expires);

                    configuration
                        .WithAutoDelete(configSettings.AutoDelete)
                        .WithPriority(configSettings.Priority);
                };
        }

        private SubscriptionConfigurationAttribute GetSubscriptionConfigurationAttributeValue(AutoSubscriberConsumerInfo subscriptionInfo)
        {
            var consumeMethod = ConsumeMethod(subscriptionInfo);
            object[] customAttributes = consumeMethod.GetCustomAttributes(typeof(SubscriptionConfigurationAttribute), true).Cast<object>().ToArray();
            return customAttributes
                             .OfType<SubscriptionConfigurationAttribute>()
                             .FirstOrDefault();
        }

        protected virtual bool IsValidMarkerType(Type markerType)
        {
            return markerType.GetTypeInfo().IsInterface && markerType.GetMethods().Any(m => m.Name == ConsumeMethodName);
        }

        protected virtual MethodInfo GetSubscribeMethodOfBus(string methodName, Type parmType)
        {
            return typeof(IBus).GetMethods()
                .Where(m => m.Name == methodName)
                .Select(m => new { Method = m, Params = m.GetParameters() })
                .Single(m => m.Params.Length == 3
                    && m.Params[0].ParameterType == typeof(string)
                    && m.Params[1].ParameterType.GetGenericTypeDefinition() == parmType
                    && m.Params[2].ParameterType == typeof(Action<ISubscriptionConfiguration>)
                   ).Method;
        }

        protected virtual AutoSubscriberConsumerAttribute GetSubscriptionAttribute(AutoSubscriberConsumerInfo consumerInfo)
        {
            var consumeMethod = ConsumeMethod(consumerInfo);

            return consumeMethod.GetCustomAttributes(typeof(AutoSubscriberConsumerAttribute), true).SingleOrDefault() as AutoSubscriberConsumerAttribute;
        }

        private MethodInfo ConsumeMethod(AutoSubscriberConsumerInfo consumerInfo)
        {
            return consumerInfo.ConcreteType.GetMethod(ConsumeMethodName, new[] { consumerInfo.MessageType }) ??
                   GetExplicitlyDeclaredInterfaceMethod(consumerInfo.MessageType);
        }

        private MethodInfo GetExplicitlyDeclaredInterfaceMethod(Type messageType)
        {
            var interfaceType = typeof (IConsume<>).MakeGenericType(messageType);
            return interfaceType.GetMethod(ConsumeMethodName);
        }

        protected virtual IEnumerable<KeyValuePair<Type, AutoSubscriberConsumerInfo[]>> GetSubscriptionInfos(IEnumerable<Type> types,Type interfaceType)
        {
            foreach (var concreteType in types.Where(t => t.GetTypeInfo().IsClass && !t.GetTypeInfo().IsAbstract))
            {
                var subscriptionInfos = concreteType.GetInterfaces()
                    .Where(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == interfaceType && !i.GetGenericArguments()[0].IsGenericParameter)
                    .Select(i => new AutoSubscriberConsumerInfo(concreteType, i, i.GetGenericArguments()[0]))
                    .ToArray();

                if (subscriptionInfos.Any())
                    yield return new KeyValuePair<Type, AutoSubscriberConsumerInfo[]>(concreteType, subscriptionInfos);
            }
        }
    }
}