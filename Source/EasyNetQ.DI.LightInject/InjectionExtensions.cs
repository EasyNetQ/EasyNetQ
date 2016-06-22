using LightInject;

namespace EasyNetQ.DI
{
    public static class InjectionExtensions
    {
        public static void RegisterAsEasyNetQContainerFactory(this IServiceContainer container)
        {
            RabbitHutch.SetContainerFactory(() => new LightInjectAdapter(container));
        }
    }
}
