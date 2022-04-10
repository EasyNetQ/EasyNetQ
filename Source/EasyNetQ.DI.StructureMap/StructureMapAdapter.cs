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

    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        throw new NotImplementedException();
    }

    public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        throw new NotImplementedException();
    }

    public IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = true)
    {
        throw new NotImplementedException();
    }

    public IServiceRegister TryRegister(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        throw new NotImplementedException();
    }

    public IServiceRegister TryRegister(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        throw new NotImplementedException();
    }

    public IServiceRegister TryRegister(Type serviceType, object implementationInstance, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        throw new NotImplementedException();
    }

    /*
    /// <inheritdoc />
    public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                registry.For<TService>(Lifecycles.Transient).ClearAll().Use<TImplementation>();
                return this;
            case Lifetime.Singleton:
                registry.For<TService>(Lifecycles.Singleton).ClearAll().Use<TImplementation>();
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    ICollectionServiceRegister ICollectionServiceRegister.Register<TService, TImplementation>(Lifetime lifetime)
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
        registry.For<TService>(Lifecycles.Singleton).ClearAll().Use(instance);
        return this;
    }

    ICollectionServiceRegister ICollectionServiceRegister.Register<TService>(TService instance)
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
                registry.For<TService>(Lifecycles.Transient).ClearAll().Use(y => factory(y.GetInstance<IServiceResolver>()));
                return this;
            case Lifetime.Singleton:
                registry.For<TService>(Lifecycles.Singleton).ClearAll().Use(y => factory(y.GetInstance<IServiceResolver>()));
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    ICollectionServiceRegister ICollectionServiceRegister.Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime)
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
    }*/

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
