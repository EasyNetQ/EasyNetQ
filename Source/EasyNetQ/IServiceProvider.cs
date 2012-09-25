using System;

namespace EasyNetQ
{
    public interface IServiceProvider
    {
        TService Resolve<TService>() where TService : class;
    }

    public interface IServiceRegister
    {
        IServiceRegister Register<TService>(Func<IServiceProvider, TService> serviceCreator) where TService : class;
    }
}