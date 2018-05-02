using System;
using Castle.Windsor;

namespace EasyNetQ.DI.Windsor
{
    public static class WindsorExtensions
    {
        public static IWindsorContainer RegisterEasyNetQ(this IWindsorContainer container, ConnectionConfiguration connectionConfiguration, AdvancedBusEventHandlers advancedBusEventHandlers, Action<IServiceRegister> registerServices)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            
            var serviceRegistry = new WindsorAdapter(container);
            serviceRegistry.RegisterBus(connectionConfiguration, advancedBusEventHandlers, registerServices);
            return container;
        }
    }
}