using System;
using EasyNetQ.ConnectionString;
using StructureMap;

namespace EasyNetQ.DI.StructureMap
{
    public static class StructureMapExtensions
    {
        public static IRegistry RegisterEasyNetQ(this IRegistry container, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory, Action<IServiceRegister> registerServices)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var serviceRegister = new StructureMapAdapter(container);
            RabbitHutch.RegisterBus(serviceRegister, connectionConfigurationFactory, registerServices);
            return container;
        }
        
        public static IRegistry RegisterEasyNetQ(this IRegistry container, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            
            return container.RegisterEasyNetQ(connectionConfigurationFactory, c => {});
        }
        
        public static IRegistry RegisterEasyNetQ(this IRegistry container, string connectionString, Action<IServiceRegister> registerServices)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            
            return container.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString), registerServices);
        }
        
        public static IRegistry RegisterEasyNetQ(this IRegistry container, string connectionString)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            
            return container.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString));
        }
    }
}