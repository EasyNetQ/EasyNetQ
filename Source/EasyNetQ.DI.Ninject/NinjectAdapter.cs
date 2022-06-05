using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ninject;
using Ninject.Activation;
using Ninject.Infrastructure;
using Ninject.Planning.Bindings;

namespace EasyNetQ.DI.Ninject;

/// <see cref="IServiceRegister"/> implementation for Ninject DI container.
public class NinjectAdapter : IServiceRegister
{
    private readonly IKernel kernel;

    /// <summary>
    /// Creates an adapter on top of <see cref="IKernel"/>.
    /// </summary>
    public NinjectAdapter(IKernel kernel)
    {
        this.kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        this.kernel.Rebind<IServiceResolver>().ToMethod(x => new NinjectResolver(x.Kernel)).InTransientScope();
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton)
    {
        kernel.Rebind(serviceType).To(implementationType).InScope(ToScope(lifetime));
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton)
    {
        var typeArguments = implementationFactory.GetType().GenericTypeArguments;
        if (typeArguments.Length != 2)
            throw new InvalidOperationException("implementationFactory should have 2 generic type arguments");

        kernel.Rebind(serviceType).ToMethod(x => implementationFactory(x.Kernel.Get<IServiceResolver>())).InScope(ToScope(lifetime));
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, object implementationInstance)
    {
        kernel.Rebind(serviceType).ToConstant(implementationInstance);
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

    private bool IsServiceRegistered(Type serviceType) => kernel.GetBindings(serviceType).Any();

    private static Func<IContext, object> ToScope(Lifetime lifetime) =>
        lifetime switch
        {
            Lifetime.Singleton => StandardScopeCallbacks.Singleton,
            Lifetime.Transient => StandardScopeCallbacks.Transient,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };

    private class NinjectResolver : IServiceResolver
    {
        private readonly IKernel kernel;

        public NinjectResolver(IKernel kernel) => this.kernel = kernel;

        public TService Resolve<TService>() where TService : class => kernel.Get<TService>();

        public IServiceResolverScope CreateScope() => new ServiceResolverScope(this);
    }
}
