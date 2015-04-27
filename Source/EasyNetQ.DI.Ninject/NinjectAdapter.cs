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
            if (!IsAlreadyRegistered<TService>())
            {
                _ninjectContainer.Bind<TService>().ToMethod(ctx => serviceCreator(this)).InSingletonScope();
            }
            return this;
        }

        public IServiceRegister Register<TService, TImplementation>() 
            where TService : class 
            where TImplementation : class, TService
        {
            if (!IsAlreadyRegistered<TService>())
            {
                _ninjectContainer.Bind<TService>().ToMethod(ctx => ctx.Kernel.Get<TImplementation>()).InSingletonScope();
            }
            return this;
        }

        /// <summary>
        /// Checking if TService can be resolved is a workaround to the issue that Ninject
        /// does not allow TService to be registered multiple times such that the behavior 
        /// of DefaultServiceProvider can be emulated using Ninject. 
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        private bool IsAlreadyRegistered<TService>() where TService : class
        {
            return _ninjectContainer.CanResolve<TService>();
        }

        public void Dispose()
        {
            _ninjectContainer.Dispose();
        }
    }
}
