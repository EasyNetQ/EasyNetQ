using System;

namespace EasyNetQ.Producer.Waiters
{
    public static class WaiterExtensions
    {
        public static IServiceRegister EnableReconnectionWithFixedDelay(this IServiceRegister serviceRegister)
        {
            serviceRegister.Register<IReconnectionWaiterFactory>(_ => new FixedDelayWaiterFactory());
            return serviceRegister;
        }

        public static IServiceRegister EnableReconnectionWithExponentialBackoffDelay(this IServiceRegister serviceRegister)
        {
            serviceRegister.Register<IReconnectionWaiterFactory>(_ => new ExponentialBackoffWaiterFactory());
            return serviceRegister;
        }
    }
}