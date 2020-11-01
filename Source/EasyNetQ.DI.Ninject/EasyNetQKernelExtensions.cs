using System;
using EasyNetQ;
using EasyNetQ.ConnectionString;
using EasyNetQ.DI;
using EasyNetQ.DI.Ninject;

// ReSharper disable once CheckNamespace
namespace Ninject
{
    public static class EasyNetQKernelExtensions
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

        public static IKernel RegisterEasyNetQ(this IKernel kernel, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return kernel.RegisterEasyNetQ(connectionConfigurationFactory, c => { });
        }

        public static IKernel RegisterEasyNetQ(this IKernel kernel, string connectionString, Action<IServiceRegister> registerServices)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return kernel.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString), registerServices);
        }

        public static IKernel RegisterEasyNetQ(this IKernel kernel, string connectionString)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return kernel.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString));
        }
    }
}
