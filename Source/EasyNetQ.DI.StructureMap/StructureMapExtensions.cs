using System;
using StructureMap;

namespace EasyNetQ.DI.StructureMap
{
    public static class StructureMapExtensions
    {
        public static IRegistry RegisterEasyNetQ(this IRegistry container, ConnectionConfiguration connectionConfiguration, Action<IServiceRegister> registerServices)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            
            var serviceRegistry = new StructureMapAdapter(container);
            serviceRegistry.RegisterBus(connectionConfiguration, registerServices);
            return container;
        }
    }
}