using System;
using Autofac;

namespace EasyNetQ.DI.Autofac
{
    public static class AutofacExtensions
    {
        public static ContainerBuilder RegisterEasyNetQ(this ContainerBuilder containerBuilder, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory, Action<IServiceRegister> registerServices)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }
            
            var serviceRegister = new AutofacAdapter(containerBuilder);
            RabbitHutch.RegisterBus(serviceRegister, connectionConfigurationFactory, registerServices);
            return containerBuilder;
        }
    }
}