using System;
using LightInject;

namespace EasyNetQ.DI.LightInject
{
    public static class LightInjectExtensions
    {
        public static IServiceContainer RegisterEasyNetQ(this IServiceContainer serviceContainer, ConnectionConfiguration connectionConfiguration, Action<IServiceRegister> registerServices) 
        {
            if (serviceContainer == null)
            {
                throw new ArgumentNullException(nameof(serviceContainer));
            }
            
            var serviceRegistry = new LightInjectAdapter(serviceContainer);
            serviceRegistry.RegisterBus(connectionConfiguration, registerServices);
            return serviceContainer;
        }
    }
}