using System;
using Castle.Windsor;

namespace EasyNetQ.DI.Windsor
{
    public static class WindsorExtensions
    {
        public static IWindsorContainer RegisterEasyNetQ(this IWindsorContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            return container.RegisterEasyNetQ(c => {});
        }
        
        public static IWindsorContainer RegisterEasyNetQ(this IWindsorContainer container, Action<IServiceRegister> registerServices)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            
            if (registerServices == null)
            {
                throw new ArgumentNullException(nameof(registerServices));
            }
            
            var serviceRegistry = new WindsorAdapter(container);
            serviceRegistry.RegisterDefaultServices();
            registerServices(serviceRegistry);
            return container;
        }
    }
}