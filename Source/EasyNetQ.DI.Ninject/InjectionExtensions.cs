using Ninject;

namespace EasyNetQ.DI
{
    public static class InjectionExtensions
    {
        public static void RegisterAsEasyNetQContainerFactory(this IKernel container)
        {
            RabbitHutch.SetContainerFactory(() => new NinjectAdapter(container));
        }
    }
}