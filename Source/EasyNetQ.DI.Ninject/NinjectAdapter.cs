using Ninject;
using Ninject.Activation;
using Ninject.Infrastructure;

namespace EasyNetQ.DI.Ninject;

/// <see cref="IServiceRegister"/> implementation for Ninject DI container.
public sealed class NinjectAdapter : IServiceRegister
{
    /// <summary>
    /// Creates an adapter on top of <see cref="IKernel"/>.
    /// </summary>
    public NinjectAdapter(IKernel kernel)
    {
        Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        Kernel.Rebind<IServiceResolver>().ToMethod(x => new NinjectResolver(x.Kernel)).InTransientScope();
    }

    public IKernel Kernel { get; }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton)
    {
        Kernel.Rebind(serviceType).To(implementationType).InScope(ToScope(lifetime));
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton)
    {
        var typeArguments = implementationFactory.GetType().GenericTypeArguments;
        if (typeArguments.Length != 2)
            throw new InvalidOperationException("implementationFactory should have 2 generic type arguments");

        Kernel.Rebind(serviceType).ToMethod(x => implementationFactory(x.Kernel.Get<IServiceResolver>())).InScope(ToScope(lifetime));
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, object implementationInstance)
    {
        Kernel.Rebind(serviceType).ToConstant(implementationInstance);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(
        Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton
    ) => IsServiceRegistered(serviceType) ? this : Register(serviceType, implementationType, lifetime);

    /// <inheritdoc />
    public IServiceRegister TryRegister(
        Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton
    ) => IsServiceRegistered(serviceType) ? this : Register(serviceType, implementationFactory, lifetime);

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, object implementationInstance)
        => IsServiceRegistered(serviceType) ? this : Register(serviceType, implementationInstance);

    private bool IsServiceRegistered(Type serviceType) => Kernel.GetBindings(serviceType).Any();

    private static Func<IContext, object> ToScope(Lifetime lifetime) =>
        lifetime switch
        {
            Lifetime.Singleton => StandardScopeCallbacks.Singleton,
            Lifetime.Transient => StandardScopeCallbacks.Transient,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };

    private sealed class NinjectResolver : IServiceResolver
    {
        private readonly IKernel kernel;

        public NinjectResolver(IKernel kernel) => this.kernel = kernel;

        public TService Resolve<TService>() where TService : class => kernel.Get<TService>();

        public IServiceResolverScope CreateScope() => new ServiceResolverScope(this);
    }
}
