using System;
using System.Reflection;
using System.Threading;

namespace EasyNetQ.AutoSubscribe
{
    public static class AutoSubscriberExtensions
    {
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