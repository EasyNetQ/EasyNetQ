using System;
using SimpleInjector;

namespace EasyNetQ.DI.SimpleInjector;

// https://simpleinjector.readthedocs.io/en/latest/using.html
/// <inheritdoc cref="EasyNetQ.DI.IServiceRegister" />
public class SimpleInjectorAdapter : IServiceRegister, ICollectionServiceRegister, IServiceResolver
{
    private readonly Container container;

    /// <summary>
    ///     Creates an adapter on top of Container
    /// </summary>
    public SimpleInjectorAdapter(Container container)
    {
        this.container = container ?? throw new ArgumentNullException(nameof(container));
        this.container.RegisterInstance<IServiceResolver>(this);
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton)
        where TService : class
        where TImplementation : class, TService
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                container.Register<TService, TImplementation>();
                return this;
            case Lifetime.Singleton:
                container.RegisterSingleton<TService, TImplementation>();
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
                container.Collection.Append<TService, TImplementation>(Lifestyle.Transient);
                return this;
            case Lifetime.Singleton:
                container.Collection.Append<TService, TImplementation>(Lifestyle.Singleton);
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    /// <inheritdoc />
    public IServiceRegister Register<TService>(TService instance) where TService : class
    {
        container.RegisterInstance(instance);
        return this;
    }

    /// <inheritdoc />
    ICollectionServiceRegister ICollectionServiceRegister.Register<TService>(TService instance)
    {
        container.Collection.AppendInstance(instance);
        return this;
    }

    public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                container.Register(() => factory(this));
                return this;
            case Lifetime.Singleton:
                container.RegisterSingleton(() => factory(this));
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    ICollectionServiceRegister ICollectionServiceRegister.Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton)
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                container.Collection.Append(() => factory(this), Lifestyle.Transient);
                return this;
            case Lifetime.Singleton:
                container.Collection.Append(() => factory(this), Lifestyle.Singleton);
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
                container.Register(serviceType, implementingType);
                return this;
            case Lifetime.Singleton:
                container.RegisterSingleton(serviceType, implementingType);
                return this;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
    }

    /// <inheritdoc />
    public TService Resolve<TService>() where TService : class
    {
        return container.GetInstance<TService>();
    }

    /// <inheritdoc />
    public IServiceResolverScope CreateScope()
    {
        return new ServiceResolverScope(this);
    }
}
