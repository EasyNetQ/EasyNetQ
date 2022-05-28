using System;
using Ninject;

namespace EasyNetQ.DI.Ninject;

/// <inheritdoc />
public class NinjectAdapter : IServiceRegister
{
    private readonly IKernel kernel;

    /// <summary>
    ///     Creates an adapter on top of IKernel
    /// </summary>
    public NinjectAdapter(IKernel kernel)
    {
        this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

        this.kernel.Rebind<IServiceResolver>().ToMethod(x => new NinjectResolver(x.Kernel)).InTransientScope();
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
    {
        if (kernel.CanResolve<TService>()) return this;

        switch (lifetime)
        {
            case Lifetime.Transient:
                kernel.Bind<TService>().To<TImplementation>().InTransientScope();
                return this;
            case Lifetime.Singleton:
                kernel.Bind<TService>().To<TImplementation>().InSingletonScope();
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService>(TService instance) where TService : class
    {
        if (kernel.CanResolve<TService>()) return this;

        kernel.Bind<TService>().ToConstant(instance);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
    {
        if (kernel.CanResolve<TService>()) return this;

        switch (lifetime)
        {
            case Lifetime.Transient:
                kernel.Bind<TService>().ToMethod(x => factory(x.Kernel.Get<IServiceResolver>())).InTransientScope();
                return this;
            case Lifetime.Singleton:
                kernel.Bind<TService>().ToMethod(x => factory(x.Kernel.Get<IServiceResolver>())).InSingletonScope();
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    /// <inheritdoc />
    public IServiceRegister Register(
        Type serviceType, Type implementingType, Lifetime lifetime = Lifetime.Singleton
    )
    {
        if (kernel.CanResolve(serviceType)) return this;

        switch (lifetime)
        {
            case Lifetime.Transient:
                kernel.Bind(serviceType).To(implementingType).InTransientScope();
                return this;
            case Lifetime.Singleton:
                kernel.Bind(serviceType).To(implementingType).InSingletonScope();
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    private class NinjectResolver : IServiceResolver
    {
        private readonly IKernel kernel;

        public NinjectResolver(IKernel kernel) => this.kernel = kernel;

        public TService Resolve<TService>() where TService : class => kernel.Get<TService>();

        public IServiceResolverScope CreateScope() => new ServiceResolverScope(this);
    }
}
