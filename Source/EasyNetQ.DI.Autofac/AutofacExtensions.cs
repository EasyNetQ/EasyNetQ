using System;
using Autofac;

namespace EasyNetQ.DI.Autofac
{
    public static class AutofacExtensions
    {
        public static ContainerBuilder RegisterEasyNetQ(this ContainerBuilder containerBuilder)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }

            return containerBuilder.RegisterEasyNetQ(c => {});
        }
        
        public static ContainerBuilder RegisterEasyNetQ(this ContainerBuilder containerBuilder, Action<IServiceRegister> registerServices)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }
            
            if (registerServices == null)
            {
                throw new ArgumentNullException(nameof(registerServices));
            }
            
            var serviceRegistry = new AutofacAdapter(containerBuilder);
            serviceRegistry.RegisterDefaultServices();
            registerServices(serviceRegistry);
            return containerBuilder;
        }
    }
}