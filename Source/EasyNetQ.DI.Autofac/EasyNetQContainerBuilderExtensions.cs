using System;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using EasyNetQ.DI;
using EasyNetQ.DI.Autofac;

// ReSharper disable once CheckNamespace
namespace Autofac
{
    public static class EasyNetQContainerBuilderExtensions
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

            return containerBuilder.RegisterEasyNetQ(connectionConfigurationFactory, c => { });
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
