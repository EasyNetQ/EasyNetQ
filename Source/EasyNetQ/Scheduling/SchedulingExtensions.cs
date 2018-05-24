using EasyNetQ.DI;

namespace EasyNetQ.Scheduling
{
    public static class SchedulingExtensions
    {
        public static IServiceRegister EnableDelayedExchangeScheduler(this IServiceRegister serviceRegister)
        {
            return serviceRegister.Register<IScheduler, DelayedExchangeScheduler>();
        }

        public static IServiceRegister EnableDeadLetterExchangeAndMessageTtlScheduler(this IServiceRegister serviceRegister)
        {
            return serviceRegister.Register<IScheduler, DeadLetterExchangeAndMessageTtlScheduler>();
        }
    }
}