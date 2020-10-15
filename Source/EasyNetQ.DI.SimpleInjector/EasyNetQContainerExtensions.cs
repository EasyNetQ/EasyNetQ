using System;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using EasyNetQ.DI;
using EasyNetQ.DI.SimpleInjector;

// ReSharper disable once CheckNamespace
namespace SimpleInjector
{
    public static class EasyNetQContainerExtensions
    {
        public static Container RegisterEasyNetQ(this Container container, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory, Action<IServiceRegister> registerServices)
        {
            return container.RegisterEasyNetQ(connectionConfigurationFactory, (r, _) => registerServices(r));
        }

        public static Container RegisterEasyNetQ(this Container container, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory, Action<IServiceRegister, ICollectionServiceRegister> registerServices)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var serviceRegister = new SimpleInjectorAdapter(container);
            RabbitHutch.RegisterBus(serviceRegister, connectionConfigurationFactory, registerServices);
            return container;
        }

        public static Container RegisterEasyNetQ(this Container container, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory)
        {
            return container.RegisterEasyNetQ(connectionConfigurationFactory, c => {});
        }
        
        public static Container RegisterEasyNetQ(this Container container, string connectionString, Action<IServiceRegister> registerServices)
        {
            return container.RegisterEasyNetQ(connectionString, (r, _) => registerServices(r));
        }

        public static Container RegisterEasyNetQ(this Container container, string connectionString, Action<IServiceRegister, ICollectionServiceRegister> registerServices)
        {
            return container.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString), registerServices);
        }

        public static Container RegisterEasyNetQ(this Container container, string connectionString)
        {
            return container.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString));
        }
    }
}
