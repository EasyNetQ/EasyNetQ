using System;
using LightInject;

namespace EasyNetQ.DI.LightInject
{
    public static class LightInjectExtensions
    {
        public static IServiceContainer RegisterEasyNetQ(this IServiceContainer serviceContainer, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory, Action<IServiceRegister> registerServices) 
        {
            if (serviceContainer == null)
            {
                throw new ArgumentNullException(nameof(serviceContainer));
            }
            
            var serviceRegister = new LightInjectAdapter(serviceContainer);
            RabbitHutch.RegisterBus(serviceRegister, connectionConfigurationFactory, registerServices);
            return serviceContainer;
        }
    }
}