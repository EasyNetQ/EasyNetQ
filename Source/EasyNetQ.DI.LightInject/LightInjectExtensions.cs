using System;
using LightInject;

namespace EasyNetQ.DI.LightInject
{
    public static class LightInjectExtensions
    {
        public static IServiceContainer RegisterEasyNetQ(this IServiceContainer serviceContainer)
        {
            if (serviceContainer == null)
            {
                throw new ArgumentNullException(nameof(serviceContainer));
            }

            return serviceContainer.RegisterEasyNetQ(c => {});
        }

        public static IServiceContainer RegisterEasyNetQ(this IServiceContainer serviceContainer, Action<IServiceRegister> registerServices) 
        {
            if (serviceContainer == null)
            {
                throw new ArgumentNullException(nameof(serviceContainer));
            }
            
            if (registerServices == null)
            {
                throw new ArgumentNullException(nameof(registerServices));
            }

            var serviceRegistry = new LightInjectAdapter(serviceContainer);
            serviceRegistry.RegisterDefaultServices();
            registerServices(serviceRegistry);
            return serviceContainer;
        }
    }
}