using System;
using Autofac;

namespace EasyNetQ.DI.Autofac
{
    public static class AutofacExtensions
    {
        public static ContainerBuilder RegisterEasyNetQ(this ContainerBuilder containerBuilder, ConnectionConfiguration connectionConfiguration, AdvancedBusEventHandlers advancedBusEventHandlers, Action<IServiceRegister> registerServices)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }
            
            var serviceRegistry = new AutofacAdapter(containerBuilder);
            serviceRegistry.RegisterBus(connectionConfiguration, advancedBusEventHandlers, registerServices);
            return containerBuilder;
        }
    }
}