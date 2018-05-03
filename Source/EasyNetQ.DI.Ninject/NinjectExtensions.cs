using System;
using Ninject;

namespace EasyNetQ.DI.Ninject
{
    public static class NinjectExtensions
    {
        public static IKernel RegisterEasyNetQ(this IKernel kernel, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory, Action<IServiceRegister> registerServices)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            var serviceRegister = new NinjectAdapter(kernel);
            RabbitHutch.RegisterBus(serviceRegister, connectionConfigurationFactory, registerServices);
            return kernel;
        }
    }
}