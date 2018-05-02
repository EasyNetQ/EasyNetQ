using System;
using SimpleInjector;

namespace EasyNetQ.DI.SimpleInjector
{
    public static class SimpleInjectorExtensions
    {
        public static Container RegisterEasyNetQ(this Container container, ConnectionConfiguration connectionConfiguration, Action<IServiceRegister> registerServices)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            
            var serviceRegistry = new SimpleInjectorAdapter(container);
            serviceRegistry.RegisterBus(connectionConfiguration, registerServices);
            return container;
        }
    }
}