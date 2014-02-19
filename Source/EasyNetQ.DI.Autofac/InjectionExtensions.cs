using Autofac;

namespace EasyNetQ.DI
{
    public static class InjectionExtensions
    {
        public static Autofac.IContainer RegisterAsEasyNetQContainerFactory(this ContainerBuilder builder)
        {
            var autofacAdapter = new AutofacAdapter(builder);
            
            RabbitHutch.SetContainerFactory(() => autofacAdapter);
            
            return autofacAdapter.Container;
        }
    }
}
