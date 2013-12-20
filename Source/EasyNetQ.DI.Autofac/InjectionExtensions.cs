using Autofac;

namespace EasyNetQ.DI
{
    public static class InjectionExtensions
    {
        public static void RegisterAsEasyNetQContainerFactory(this ContainerBuilder builder)
        {
            var autofacAdapter = new AutofacAdapter(builder);
            RabbitHutch.SetContainerFactory(() => autofacAdapter);
        }
    }
}
