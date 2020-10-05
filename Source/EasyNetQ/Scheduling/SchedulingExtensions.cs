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
            return serviceRegister.Register<IScheduler>(
                x => new DeadLetterExchangeAndMessageTtlScheduler(
                    x.Resolve<IAdvancedBus>(),
                    x.Resolve<IConventions>(),
                    x.Resolve<IMessageDeliveryModeStrategy>()
                )
            );
        }

        public static IServiceRegister EnableLegacyDeadLetterExchangeAndMessageTtlScheduler(this IServiceRegister serviceRegister)
        {
            return serviceRegister.Register<IScheduler>(
                x => new DeadLetterExchangeAndMessageTtlScheduler(
                    x.Resolve<IAdvancedBus>(),
                    x.Resolve<IConventions>(),
                    x.Resolve<IMessageDeliveryModeStrategy>(),
                    true
                )
            );
        }
    }
}
