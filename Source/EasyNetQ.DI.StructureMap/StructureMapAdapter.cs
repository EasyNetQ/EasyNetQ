using System;
using StructureMap;
using StructureMap.Pipeline;

namespace EasyNetQ.DI.StructureMap;

/// <see cref="IServiceRegister"/> implementation for StructureMap DI container.
public class StructureMapAdapter : IServiceRegister
{
    private readonly IRegistry registry;

    /// <summary>
    /// Creates an adapter on top of <see cref="IRegistry"/>.
    /// </summary>
    public StructureMapAdapter(IRegistry registry)
    {
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));

        this.registry.For<IServiceResolver>(Lifecycles.Container).Use<StructureMapResolver>();
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        if (replace)
            registry.For(serviceType, ToLifetime(lifetime)).ClearAll().Use(implementationType);
        else
            registry.For(serviceType, ToLifetime(lifetime)).Use(implementationType);

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        if (replace)
            registry.For(serviceType, ToLifetime(lifetime)).ClearAll().Use(y => implementationFactory(y.GetInstance<IServiceResolver>()));
        else
            registry.For(serviceType, ToLifetime(lifetime)).Use(y => implementationFactory(y.GetInstance<IServiceResolver>()));

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = true)
    {
        if (replace)
            registry.For(serviceType, Lifecycles.Singleton).ClearAll().Use(implementationInstance);
        else
            registry.For(serviceType, Lifecycles.Singleton).Use(implementationInstance);

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        //TODO: Figure out how to get current registrations from StructureMap
        //var producer = container.GetRegistration(serviceType, throwOnFailure: false);

        if (mode == RegistrationCompareMode.ServiceType)
        {
            //if (producer == null)
                Register(serviceType, implementationType, lifetime);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            //if (producer == null || producer.Registration.ImplementationType != implementationType)
                Register(serviceType, implementationType, lifetime);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        //TODO: Figure out how to get current registrations from StructureMap
        //var producer = container.GetRegistration(serviceType, throwOnFailure: false);

        if (mode == RegistrationCompareMode.ServiceType)
        {
            //if (producer == null)
                Register(serviceType, implementationFactory, lifetime);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            Type[] typeArguments = implementationFactory.GetType().GenericTypeArguments;
            if (typeArguments.Length != 2)
                throw new InvalidOperationException("implementationFactory should be of type Func<IServiceResolver, T>");
            var implementationType = typeArguments[1];
            //if (producer == null || producer.Registration.ImplementationType != implementationType)
                Register(serviceType, implementationFactory, lifetime);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, object implementationInstance, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        //TODO: Figure out how to get current registrations from StructureMap
        //var producer = container.GetRegistration(serviceType, throwOnFailure: false);

        if (mode == RegistrationCompareMode.ServiceType)
        {
            //if (producer == null)
                Register(serviceType, implementationInstance);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            var implementationType = implementationInstance.GetType();
            //if (producer == null || producer.Registration.ImplementationType != implementationType)
                Register(serviceType, implementationInstance);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return this;
    }

    private static ILifecycle ToLifetime(Lifetime lifetime)
    {
        return lifetime switch
        {
            Lifetime.Singleton => Lifecycles.Singleton,
            //Lifetime.Scoped => Lifecycles.Singleton, //TODO,
            Lifetime.Transient => Lifecycles.Transient,
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };
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
