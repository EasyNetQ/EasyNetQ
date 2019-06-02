using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.AutoSubscribe
{
    public static class AutoSubscriberExtensions
    {
        /// <summary>
        /// Registers all async consumers in passed assembly. The actual Subscriber instances is
        /// created using <seealso cref="AutoSubscriber.AutoSubscriberMessageDispatcher"/>. The SubscriptionId per consumer
        /// method is determined by <seealso cref="AutoSubscriber.GenerateSubscriptionId"/> or if the method
        /// is marked with <see cref="AutoSubscriberConsumerAttribute"/> with a custom SubscriptionId.
        /// </summary>
        /// <param name="autoSubscriber">The autoSubscriber instance.</param>
        /// <param name="assemblies">The assemblies to scan for consumers.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task<IDisposable> SubscribeAsync(this AutoSubscriber autoSubscriber, Assembly[] assemblies, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckAny(assemblies, nameof(assemblies), "No assemblies specified.");

            return autoSubscriber.SubscribeAsync(assemblies.SelectMany(a => a.GetTypes()).ToArray(), cancellationToken);
        }

        public static IDisposable Subscribe(this AutoSubscriber autoSubscriber, Assembly[] assemblies, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(autoSubscriber, "autoSubscriber");

            return autoSubscriber.SubscribeAsync(assemblies, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        public static IDisposable Subscribe(this AutoSubscriber autoSubscriber, Type[] consumerTypes, CancellationToken cancellationToken = default)
        {
            Preconditions.CheckNotNull(autoSubscriber, "autoSubscriber");

            return autoSubscriber.SubscribeAsync(consumerTypes, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }
    }
}
