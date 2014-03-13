using System;
using Autofac;

namespace EasyNetQ.DI
{
    public static class InjectionExtensions
    {
        public static Autofac.IContainer RegisterAsEasyNetQContainerFactory(this ContainerBuilder builder, Func<IBus> busCreator)
        {
            var adapter = new AutofacAdapter(builder);
            
            RabbitHutch.SetContainerFactory(() => adapter);

            var container = adapter.Container;

            var bus = busCreator();

            adapter.Register(provider => bus);

            return container;
        }

    }
}
