using System;
using StructureMap;

namespace EasyNetQ.DI.StructureMap
{
    public static class StructureMapExtensions
    {
        public static Container RegisterEasyNetQ(this Container container, ConnectionConfiguration connectionConfiguration, AdvancedBusEventHandlers advancedBusEventHandlers, Action<IServiceRegister> registerServices)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            
            var serviceRegistry = new StructureMapAdapter(container);
            serviceRegistry.RegisterBus(connectionConfiguration, advancedBusEventHandlers, registerServices);
            return container;
        }
    }
}