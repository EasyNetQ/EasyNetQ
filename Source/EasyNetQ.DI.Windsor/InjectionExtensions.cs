using Castle.Windsor;

namespace EasyNetQ.DI
{
    public static class InjectionExtensions
    {
        public static void RegisterAsEasyNetQContainerFactory(this IWindsorContainer container)
        {
            RabbitHutch.SetContainerFactory(() => new WindsorAdapter(container));
        }
    }
}