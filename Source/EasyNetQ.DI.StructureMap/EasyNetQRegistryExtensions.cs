using System;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using EasyNetQ.DI;
using EasyNetQ.DI.StructureMap;

// ReSharper disable once CheckNamespace
namespace StructureMap
{
    public static class EasyNetQRegistryExtensions
    {
        public static IRegistry RegisterEasyNetQ(this IRegistry registry, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory, Action<IServiceRegister> registerServices)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            var serviceRegister = new StructureMapAdapter(registry);
            RabbitHutch.RegisterBus(serviceRegister, connectionConfigurationFactory, registerServices);
            return registry;
        }

        public static IRegistry RegisterEasyNetQ(this IRegistry registry, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            return registry.RegisterEasyNetQ(connectionConfigurationFactory, c => { });
        }

        public static IRegistry RegisterEasyNetQ(this IRegistry registry, string connectionString, Action<IServiceRegister> registerServices)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            return registry.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString), registerServices);
        }

        public static IRegistry RegisterEasyNetQ(this IRegistry registry, string connectionString)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            return registry.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString));
        }
    }
}
