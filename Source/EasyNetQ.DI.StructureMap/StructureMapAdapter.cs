using System;
using StructureMap;
using StructureMap.Pipeline;

namespace EasyNetQ.DI.StructureMap;

/// <inheritdoc />
public class StructureMapAdapter : IServiceRegister
{
    private readonly IRegistry registry;

    /// <summary>
    ///     Creates an adapter on top of IRegistry
    /// </summary>
    public StructureMapAdapter(IRegistry registry)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));

        this.registry.For<IServiceResolver>(Lifecycles.Container).Use<StructureMapResolver>();
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                registry.For<TService>(Lifecycles.Transient).Use<TImplementation>();
                return this;
            case Lifetime.Singleton:
                registry.For<TService>(Lifecycles.Singleton).Use<TImplementation>();
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService>(TService instance) where TService : class
    {
        registry.For<TService>(Lifecycles.Singleton).Use(instance);
        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                registry.For<TService>(Lifecycles.Transient).Use(y => factory(y.GetInstance<IServiceResolver>()));
                return this;
            case Lifetime.Singleton:
                registry.For<TService>(Lifecycles.Singleton).Use(y => factory(y.GetInstance<IServiceResolver>()));
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
        switch (lifetime)
        {
            case Lifetime.Transient:
                registry.For(serviceType, Lifecycles.Transient).Use(implementingType);
                return this;
            case Lifetime.Singleton:
                registry.For(serviceType, Lifecycles.Singleton).Use(implementingType);
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    private class StructureMapResolver : IServiceResolver
    {
        protected readonly IContainer Container;

        public StructureMapResolver(IContainer container)
        {
            Container = container;
        }

        public TService Resolve<TService>() where TService : class
        {
            return Container.GetInstance<TService>();
        }

        public IServiceResolverScope CreateScope()
        {
            return new StructureMapResolverScope(Container.GetNestedContainer());
        }
    }

    private class StructureMapResolverScope : StructureMapResolver, IServiceResolverScope
    {
        public StructureMapResolverScope(IContainer container) : base(container)
        {
        }

        public void Dispose()
        {
            Container.Dispose();
        }
    }
}
