using System;
using Ninject;

namespace EasyNetQ.DI.Ninject
{
    public class NinjectAdapter : IServiceRegister
    {
        private readonly IKernel kernel;

        public NinjectAdapter(IKernel kernel)
        {
            this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

            this.kernel.Rebind<IServiceResolver>().ToMethod(x => new NinjectResolver(x.Kernel)).InTransientScope();
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            kernel.Rebind<TService>().ToConstant(instance);
            return this;
        }

        /// <inheritdoc />
        public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    kernel.Rebind<TService>().ToMethod(x => factory(x.Kernel.Get<IServiceResolver>())).InTransientScope();
                    return this;
                case Lifetime.Singleton:
                    kernel.Rebind<TService>().ToMethod(x => factory(x.Kernel.Get<IServiceResolver>())).InSingletonScope();
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
        }

        private class NinjectResolver : IServiceResolver
        {
            private readonly IKernel kernel;

            public NinjectResolver(IKernel kernel)
            {
                this.kernel = kernel;
            }

            public TService Resolve<TService>() where TService : class
            {
                return kernel.Get<TService>();
            }

            public IServiceResolverScope CreateScope()
            {
                return new ServiceResolverScope(this);
            }
        }
    }
}
