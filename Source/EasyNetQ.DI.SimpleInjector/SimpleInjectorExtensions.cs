using System;
using SimpleInjector;

namespace EasyNetQ.DI.SimpleInjector
{
    public static class SimpleInjectorExtensions
    {
        public static Container RegisterEasyNetQ(this Container container, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory, Action<IServiceRegister> registerServices)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var serviceRegister = new SimpleInjectorAdapter(container);
            RabbitHutch.RegisterBus(serviceRegister, connectionConfigurationFactory, registerServices);
            return container;
        }
    }
}