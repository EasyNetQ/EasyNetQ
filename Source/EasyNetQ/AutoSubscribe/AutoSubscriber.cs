using EasyNetQ.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.AutoSubscribe
{
    /// <summary>
    /// Lets you scan assemblies for implementations of <see cref="IConsume{T}"/> so that
    /// these will get registered as subscribers in the bus.
    /// </summary>
    public class AutoSubscriber
    {
        private static readonly MethodInfo AutoSubscribeAsyncConsumerMethodInfo = typeof(AutoSubscriber).GetMethod(nameof(AutoSubscribeAsyncConsumerAsync), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo AutoSubscribeConsumerMethodInfo = typeof(AutoSubscriber).GetMethod(nameof(AutoSubscribeConsumerAsync), BindingFlags.Instance | BindingFlags.NonPublic);

        protected readonly IBus Bus;

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

            Bus = bus;
            SubscriptionIdPrefix = subscriptionIdPrefix;
            AutoSubscriberMessageDispatcher = new DefaultAutoSubscriberMessageDispatcher();
            GenerateSubscriptionId = DefaultSubscriptionIdGenerator;
            ConfigureSubscriptionConfiguration = subscriptionConfiguration => { };
        }

        /// <summary>
        /// Registers all async consumers in passed assembly. The actual Subscriber instances is
        /// created using <seealso cref="AutoSubscriberMessageDispatcher"/>. The SubscriptionId per consumer
        /// method is determined by <seealso cref="GenerateSubscriptionId"/> or if the method
        /// is marked with <see cref="AutoSubscriberConsumerAttribute"/> with a custom SubscriptionId.
        /// </summary>
        /// <param name="consumerTypes">The types to register as consumers.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public virtual async Task<IDisposable> SubscribeAsync(Type[] consumerTypes, CancellationToken cancellationToken = default)
        {
            var subscriptions = new List<IDisposable>();

            foreach (var subscriberConsumerInfo in GetSubscriberConsumerInfos(consumerTypes, typeof(IConsumeAsync<>)))
            {
                var awaitableSubscriptionResult = (AwaitableDisposable<ISubscriptionResult>)AutoSubscribeAsyncConsumerMethodInfo
                    .MakeGenericMethod(subscriberConsumerInfo.MessageType, subscriberConsumerInfo.ConcreteType)
                    .Invoke(this, new object[] { subscriberConsumerInfo, cancellationToken });

                subscriptions.Add(await awaitableSubscriptionResult.ConfigureAwait(false));
            }

            foreach (var subscriberConsumerInfo in GetSubscriberConsumerInfos(consumerTypes, typeof(IConsume<>)))
            {
                var awaitableSubscriptionResult = (AwaitableDisposable<ISubscriptionResult>)AutoSubscribeConsumerMethodInfo
                    .MakeGenericMethod(subscriberConsumerInfo.MessageType, subscriberConsumerInfo.ConcreteType)
                    .Invoke(this, new object[] { subscriberConsumerInfo, cancellationToken });

                subscriptions.Add(await awaitableSubscriptionResult.ConfigureAwait(false));
            }

            subscriptions.Reverse();
            return new AutoSubscribeDisposable(subscriptions);
        }

        private sealed class AutoSubscribeDisposable : IDisposable
        {
            private readonly List<IDisposable> subscriptions;

            public AutoSubscribeDisposable(List<IDisposable> subscriptions)
            {
                this.subscriptions = subscriptions;
            }

            public void Dispose()
            {
                foreach (var subscription in subscriptions)
                {
                    subscription.Dispose();
                }
            }
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

        private AwaitableDisposable<ISubscriptionResult> AutoSubscribeAsyncConsumerAsync<TMesage, TConsumerAsync>(AutoSubscriberConsumerInfo subscriptionInfo, CancellationToken cancellationToken)
            where TMesage : class
            where TConsumerAsync : class, IConsumeAsync<TMesage>
        {
            var subscriptionAttribute = GetSubscriptionAttribute(subscriptionInfo);
            var subscriptionId = subscriptionAttribute != null ? subscriptionAttribute.SubscriptionId : GenerateSubscriptionId(subscriptionInfo);
            var configureSubscriptionAction = GenerateConfigurationAction(subscriptionInfo);

            return Bus.PubSub.SubscribeAsync<TMesage>(
                subscriptionId,
                (m, c) => AutoSubscriberMessageDispatcher.DispatchAsync<TMesage, TConsumerAsync>(m, c),
                configureSubscriptionAction,
                cancellationToken
            );
        }

        private AwaitableDisposable<ISubscriptionResult> AutoSubscribeConsumerAsync<TMesage, TConsumer>(AutoSubscriberConsumerInfo subscriptionInfo, CancellationToken cancellationToken)
            where TMesage : class
            where TConsumer : class, IConsume<TMesage>
        {
            var subscriptionAttribute = GetSubscriptionAttribute(subscriptionInfo);
            var subscriptionId = subscriptionAttribute != null ? subscriptionAttribute.SubscriptionId : GenerateSubscriptionId(subscriptionInfo);
            var configureSubscriptionAction = GenerateConfigurationAction(subscriptionInfo);

            var asyncDispatcher = TaskHelpers.FromAction<TMesage>((m, c) => AutoSubscriberMessageDispatcher.Dispatch<TMesage, TConsumer>(m, c));

            return Bus.PubSub.SubscribeAsync(
                subscriptionId,
                asyncDispatcher,
                configureSubscriptionAction,
                cancellationToken
            );
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

        private static Action<ISubscriptionConfiguration> TopicInfo(AutoSubscriberConsumerInfo subscriptionInfo)
        {
            var topics = GetTopAttributeValues(subscriptionInfo);
            if (topics.Length != 0)
            {
                return GenerateConfigurationFromTopics(topics);
            }
            return configuration => configuration.WithTopic("#");
        }

        private static Action<ISubscriptionConfiguration> GenerateConfigurationFromTopics(string[] topics)
        {
            return configuration =>
                {
                    foreach (var topic in topics)
                    {
                        configuration.WithTopic(topic);
                    }
                };
        }

        private static string[] GetTopAttributeValues(AutoSubscriberConsumerInfo subscriptionInfo)
        {
            var consumeMethod = subscriptionInfo.ConsumeMethod;
            return consumeMethod.GetCustomAttributes(typeof(ForTopicAttribute), true)
                             .OfType<ForTopicAttribute>()
                             .Select(a => a.Topic)
                             .ToArray();
        }

        private static Action<ISubscriptionConfiguration> AutoSubscriberConsumerInfo(AutoSubscriberConsumerInfo subscriptionInfo)
        {
            var configSettings = GetSubscriptionConfigurationAttributeValue(subscriptionInfo);
            if (configSettings == null)
            {
                return subscriptionConfiguration => { };
            }
            return configuration =>
                {
                    if (configSettings.PrefetchCount > 0)
                        configuration.WithPrefetchCount(configSettings.PrefetchCount);

                    if (configSettings.Expires > 0 )
                        configuration.WithExpires(configSettings.Expires);

                    configuration
                        .WithAutoDelete(configSettings.AutoDelete)
                        .WithPriority(configSettings.Priority);
                };
        }

        private static SubscriptionConfigurationAttribute GetSubscriptionConfigurationAttributeValue(AutoSubscriberConsumerInfo subscriptionInfo)
        {
            var customAttributes = subscriptionInfo.ConsumeMethod.GetCustomAttributes(typeof(SubscriptionConfigurationAttribute), true);
            return customAttributes
                             .OfType<SubscriptionConfigurationAttribute>()
                             .FirstOrDefault();
        }

        protected virtual AutoSubscriberConsumerAttribute GetSubscriptionAttribute(AutoSubscriberConsumerInfo consumerInfo)
        {
            return consumerInfo.ConsumeMethod
                .GetCustomAttributes(typeof(AutoSubscriberConsumerAttribute), true)
                .SingleOrDefault() as AutoSubscriberConsumerAttribute;
        }

        protected virtual IEnumerable<AutoSubscriberConsumerInfo> GetSubscriberConsumerInfos(IEnumerable<Type> types, Type interfaceType)
        {
            return types.Where(t => t.GetTypeInfo().IsClass && !t.GetTypeInfo().IsAbstract)
                        .SelectMany(t => t.GetInterfaces().Where(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == interfaceType && !i.GetGenericArguments()[0].IsGenericParameter)
                        .Select(i => new AutoSubscriberConsumerInfo(t, i, i.GetGenericArguments()[0])));
        }
    }
}
