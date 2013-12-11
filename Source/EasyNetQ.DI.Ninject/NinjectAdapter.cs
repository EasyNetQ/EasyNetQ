using System;
using Ninject;

namespace EasyNetQ.DI
{
    public class NinjectAdapter : IContainer, IDisposable
    {
        private readonly IKernel _ninjectContainer;

        public NinjectAdapter(IKernel ninjectContainer)
        {
            _ninjectContainer = ninjectContainer;
        }

        public TService Resolve<TService>() where TService : class
        {
            return _ninjectContainer.Get<TService>();
        }

        public IServiceRegister Register<TService>(Func<IServiceProvider, TService> serviceCreator) where TService : class
        {
            _ninjectContainer.Bind<TService>().ToMethod(ctx => serviceCreator(this)).InSingletonScope();
            return this;
        }

        public IServiceRegister Register<TService, TImplementation>() 
            where TService : class 
            where TImplementation : class, TService
        {
            _ninjectContainer.Bind<TService>().ToMethod(ctx => ctx.Kernel.Get<TImplementation>()).InSingletonScope();
            return this;
        }

        public void Dispose()
        {
            _ninjectContainer.Dispose();
        }
    }
}
