using EasyNetQ;
using EasyNetQ.ConnectionString;
using EasyNetQ.DI;
using EasyNetQ.DI.Ninject;

// ReSharper disable once CheckNamespace
namespace Ninject;

public static class EasyNetQKernelExtensions
{
    public static IKernel RegisterEasyNetQ(this IKernel kernel, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory, Action<IServiceRegister> registerServices)
    {
        var serviceRegister = new NinjectAdapter(kernel);
        RabbitHutch.RegisterBus(serviceRegister, connectionConfigurationFactory, registerServices);
        return kernel;
    }

    public static IKernel RegisterEasyNetQ(this IKernel kernel, Func<IServiceResolver, ConnectionConfiguration> connectionConfigurationFactory)
    {
        return kernel.RegisterEasyNetQ(connectionConfigurationFactory, _ => { });
    }

    public static IKernel RegisterEasyNetQ(this IKernel kernel, string connectionString, Action<IServiceRegister> registerServices)
    {
        return kernel.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString), registerServices);
    }

    public static IKernel RegisterEasyNetQ(this IKernel kernel, string connectionString)
    {
        return kernel.RegisterEasyNetQ(c => c.Resolve<IConnectionStringParser>().Parse(connectionString));
    }
}
