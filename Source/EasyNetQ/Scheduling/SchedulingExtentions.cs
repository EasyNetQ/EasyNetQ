namespace EasyNetQ.Scheduling
{
    public static class SchedulingExtentions
    {
        public static IServiceRegister EnableDelayedExchangeScheduler(this IServiceRegister serviceRegister)
        {
            return serviceRegister.Register<IScheduler, DelayedExchangeScheduler>();
        }

        public static IServiceRegister EnableDeadLetterExchangeAndMessageTtlScheduler(this IServiceRegister serviceRegister)
        {
            return serviceRegister.Register<IScheduler, DeadLetterExchangeAndMessageTtlScheduler>();
        }

        public static IServiceRegister EnableExternalSchedulerV2(this IServiceRegister serviceRegister)
        {
            return serviceRegister.Register<IScheduler, ExternalSchedulerV2>();
        }
    }
}