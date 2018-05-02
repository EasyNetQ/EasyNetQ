using System;
using SimpleInjector;

namespace EasyNetQ.DI.SimpleInjector
{
    public static class SimpleInjectorExtensions
    {
        public static Container RegisterEasyNetQ(this Container container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            return container.RegisterEasyNetQ(c => {});
        }
        
        public static Container RegisterEasyNetQ(this Container container, Action<IServiceRegister> registerServices)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            
            if (registerServices == null)
            {
                throw new ArgumentNullException(nameof(registerServices));
            }
            
            var serviceRegistry = new SimpleInjectorAdapter(container);
            serviceRegistry.RegisterDefaultServices();
            registerServices(serviceRegistry);
            return container;
        }
    }
}