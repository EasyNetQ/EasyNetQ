using System;
using Ninject;

namespace EasyNetQ.DI.Ninject
{
    public class NinjectAdapter : IServiceRegister, IServiceResolver
    {
        private readonly IKernel kernel;

        public NinjectAdapter(IKernel kernel)
        {
            this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

            this.kernel.Rebind<IServiceResolver, IServiceRegister>().ToConstant(this);
        }

        public TService Resolve<TService>() where TService : class
        {
            return kernel.Get<TService>();
        }

        public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    kernel.Rebind<TService>().To<TImplementation>().InTransientScope();
                    return this;
                case Lifetime.Singleton:
                    kernel.Rebind<TService>().To<TImplementation>().InSingletonScope();
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
        }

        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            kernel.Rebind<TService>().ToConstant(instance);
            return this;
        }
    }
}
