using SimpleInjector;

namespace EasyNetQ.DI
{
    public static class InjectionExtensions
    {
        public static void RegisterAsEasyNetQContainerFactory(this Container container)
        {
            RabbitHutch.SetContainerFactory(() => new SimpleInjectorAdapter(container));
        }
    }
}
