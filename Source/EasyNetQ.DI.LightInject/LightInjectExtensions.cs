using System;
using EasyNetQ.ConnectionString;
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
        
        public static IServiceContainer RegisterEasyNetQ(this IServiceContainer serviceContainer, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory)
        {
            if (serviceContainer == null)
            {
                throw new ArgumentNullException(nameof(serviceContainer));
            }
            
            return serviceContainer.RegisterEasyNetQ(connectionConfigurationFactory, c => {});
        }
        
        public static IServiceContainer RegisterEasyNetQ(this IServiceContainer serviceContainer, string connectionString, Action<IServiceRegister> registerServices)
        {
            if (serviceContainer == null)
            {
                throw new ArgumentNullException(nameof(serviceContainer));
            }
            
            return serviceContainer.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString), registerServices);
        }
        
        public static IServiceContainer RegisterEasyNetQ(this IServiceContainer serviceContainer, string connectionString)
        {
            if (serviceContainer == null)
            {
                throw new ArgumentNullException(nameof(serviceContainer));
            }
            
            return serviceContainer.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString));
        }
    }
}