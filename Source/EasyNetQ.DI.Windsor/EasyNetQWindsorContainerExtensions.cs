using System;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using EasyNetQ.DI;
using EasyNetQ.DI.Windsor;

// ReSharper disable once CheckNamespace
namespace Castle.Windsor
{
    public static class EasyNetQWindsorContainerExtensions
    {
        public static IWindsorContainer RegisterEasyNetQ(this IWindsorContainer container, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory, Action<IServiceRegister> registerServices)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var serviceRegister = new WindsorAdapter(container);
            RabbitHutch.RegisterBus(serviceRegister, connectionConfigurationFactory, registerServices);
            return container;
        }

        public static IWindsorContainer RegisterEasyNetQ(this IWindsorContainer container, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            return container.RegisterEasyNetQ(connectionConfigurationFactory, c => { });
        }

        public static IWindsorContainer RegisterEasyNetQ(this IWindsorContainer container, string connectionString, Action<IServiceRegister> registerServices)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            return container.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString), registerServices);
        }

        public static IWindsorContainer RegisterEasyNetQ(this IWindsorContainer container, string connectionString)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            return container.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString));
        }
    }
}
