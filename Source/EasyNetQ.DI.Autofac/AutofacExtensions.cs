using System;
using Autofac;
using EasyNetQ.ConnectionString;

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
        
        public static ContainerBuilder RegisterEasyNetQ(this ContainerBuilder containerBuilder, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }
            
            return containerBuilder.RegisterEasyNetQ(connectionConfigurationFactory, c => {});
        }
        
        public static ContainerBuilder RegisterEasyNetQ(this ContainerBuilder containerBuilder, string connectionString, Action<IServiceRegister> registerServices)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }
            
            return containerBuilder.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString), registerServices);
        }
        
        public static ContainerBuilder RegisterEasyNetQ(this ContainerBuilder containerBuilder, string connectionString)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }
            
            return containerBuilder.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString));
        }
    }
}