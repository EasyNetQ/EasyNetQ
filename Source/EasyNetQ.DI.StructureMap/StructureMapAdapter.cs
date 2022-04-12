using System;
using System.Linq;
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
            registry.For(serviceType, ToLifecycle(lifetime)).ClearAll().Use(implementationType);
        else
            registry.For(serviceType, ToLifecycle(lifetime)).Use(implementationType);

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        if (replace)
            registry.For(serviceType, ToLifecycle(lifetime)).ClearAll().Use(y => implementationFactory(y.GetInstance<IServiceResolver>()));
        else
            registry.For(serviceType, ToLifecycle(lifetime)).Use(y => implementationFactory(y.GetInstance<IServiceResolver>()));

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
        if (mode == RegistrationCompareMode.ServiceType || mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            // StructureMap has only generic API for UseIfNone, so there is a bit reflection here
            // registry.For<serviceType>(ToLifecycle(lifetime)).UseIfNone<implementationType>();
            var createPluginFamilyExpression = typeof(IProfileRegistry).GetMethod("For", new[] { typeof(ILifecycle) }).MakeGenericMethod(serviceType).Invoke(registry, new[] { ToLifecycle(lifetime) });
            createPluginFamilyExpression.GetType().GetMethod("UseIfNone", Type.EmptyTypes).MakeGenericMethod(implementationType).Invoke(createPluginFamilyExpression, Array.Empty<object>());
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            //TODO: UseIfNone for collections
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
        Type[] typeArguments = implementationFactory.GetType().GenericTypeArguments;
        if (typeArguments.Length != 2)
            throw new InvalidOperationException("implementationFactory should be of type Func<IServiceResolver, T>");
        var implementationType = typeArguments[1];

        if (mode == RegistrationCompareMode.ServiceType || mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            // StructureMap has only generic API for UseIfNone, so there is a bit reflection here
            // registry.For<serviceType>(ToLifecycle(lifetime)).UseIfNone<implementationType>("", c => implementationFactory(c.GetInstance<IServiceResolver>()));
            var createPluginFamilyExpression = typeof(IProfileRegistry).GetMethod("For", new[] { typeof(ILifecycle) }).MakeGenericMethod(serviceType).Invoke(registry, new[] { ToLifecycle(lifetime) });
            var useIfNone = createPluginFamilyExpression.GetType()
                .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .Where(m => m.Name == "UseIfNone" && m.ToString().Contains("(System.String, System.Func`2[StructureMap.IContext,T])"))
                .Single();

            var implementationFactoryAdapterType = typeof(ImplementationFactoryAdapter<>).MakeGenericType(implementationType);

            useIfNone.MakeGenericMethod(implementationType).Invoke(createPluginFamilyExpression, new object[]
            {
                string.Empty,
                Delegate.CreateDelegate(
                    typeof(Func<,>).MakeGenericType(typeof(IContext), implementationType),
                    Activator.CreateInstance(implementationFactoryAdapterType, implementationFactory),
                    implementationFactoryAdapterType.GetMethod("Resolve"))
            });
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            //TODO: UseIfNone for collections
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
        if (mode == RegistrationCompareMode.ServiceType || mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            // StructureMap has only generic API for UseIfNone, so there is a bit reflection here
            // registry.For<serviceType>().UseIfNone(implementationInstance);
            var createPluginFamilyExpression = typeof(IProfileRegistry).GetMethod("For", new[] { typeof(ILifecycle) }).MakeGenericMethod(serviceType).Invoke(registry, new object[] { null });
            createPluginFamilyExpression.GetType().GetMethod("UseIfNone", new[] { serviceType }).Invoke(createPluginFamilyExpression, new[] { implementationInstance });
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            var implementationType = implementationInstance.GetType();
            //TODO: UseIfNone for collections
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return this;
    }

    private static ILifecycle ToLifecycle(Lifetime lifetime)
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

    private class ImplementationFactoryAdapter<T>
    {
        private readonly Func<IServiceResolver, object> _implementationFactory;

        public ImplementationFactoryAdapter(Func<IServiceResolver, object> implementationFactory)
        {
            _implementationFactory = implementationFactory;
        }

        public T Resolve(IContext context) => (T)_implementationFactory(context.GetInstance<IServiceResolver>());
    }
}
