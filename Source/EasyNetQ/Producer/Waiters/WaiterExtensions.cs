using System;

namespace EasyNetQ.Producer.Waiters
{
    public static class WaiterExtensions
    {
        public static IServiceRegister EnableReconnectionWithFixedDelay(this IServiceRegister serviceRegister, TimeSpan delay)
        {
            serviceRegister.Register<IReconnectionWaiterFactory>(_ => new FixedDelayWaiterFactory((int) delay.TotalMilliseconds));
            return serviceRegister;
        }

        public static IServiceRegister EnableReconnectionWithExponentialBackoffDelay(this IServiceRegister serviceRegister, TimeSpan initialDelay)
        {
            serviceRegister.Register<IReconnectionWaiterFactory>(_ => new ExponentialBackoffWaiterFactory((int)initialDelay.TotalMilliseconds));
            return serviceRegister;
        }
    }
}