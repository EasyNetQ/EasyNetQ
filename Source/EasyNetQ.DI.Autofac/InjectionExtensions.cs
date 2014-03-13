using System;
using Autofac;

namespace EasyNetQ.DI
{
    public static class InjectionExtensions
    {
        public static Autofac.IContainer RegisterAsEasyNetQContainerFactory(this ContainerBuilder builder, Func<IBus> busCreator)
        {
            var autofacAdapter = new AutofacAdapter(builder);
            
            RabbitHutch.SetContainerFactory(() => autofacAdapter);

            IBus bus = busCreator();

            autofacAdapter.Register(provider => bus);

            return autofacAdapter.Container;
        }
    }
}
