using System;
using Castle.Windsor;

namespace EasyNetQ.DI.Windsor
{
    public static class WindsorExtensions
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
    }
}