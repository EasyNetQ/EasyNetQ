using System;
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
    }
}