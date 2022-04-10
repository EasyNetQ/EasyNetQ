#if LIGHT_INJECT_PACKAGE
using LightInject;
#else
using EasyNetQ.LightInject;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

#if LIGHT_INJECT_PACKAGE
namespace EasyNetQ.DI.LightInject;
#else
namespace EasyNetQ.DI;
#endif

/// <inheritdoc cref="IServiceRegister" />
#if LIGHT_INJECT_PACKAGE
public
#else
internal
#endif
class LightInjectAdapter : IServiceRegister
{
    private readonly IServiceRegistry serviceRegistry;
    private long counter;

    /// <summary>
    ///     Creates LightInjectAdapter
    /// </summary>
    public LightInjectAdapter(IServiceRegistry serviceRegistry)
    {
        this.serviceRegistry = serviceRegistry ?? throw new ArgumentNullException(nameof(serviceRegistry));

        serviceRegistry.Register<IServiceResolver>(x => new LightInjectResolver(x, serviceRegistry), new PerRequestLifeTime());
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        if (replace)
        {
            serviceRegistry.Register(serviceType, implementationType, ToLifetime(lifetime));
        }
        else
        {
            serviceRegistry.Register(serviceType, implementationType, GetServiceName(), ToLifetime(lifetime));
        }

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, Func<IServiceResolver, object> implementationFactory, Lifetime lifetime = Lifetime.Singleton, bool replace = true)
    {
        var serviceRegistration = new ServiceRegistration
        {
            ServiceType = serviceType,
            FactoryExpression = (Func<IServiceFactory, object>)(x => implementationFactory((IServiceResolver)x.GetInstance(typeof(IServiceResolver)))),
            ServiceName = replace ? string.Empty : GetServiceName(),
            Lifetime = ToLifetime(lifetime),
        };
        serviceRegistry.Register(serviceRegistration);

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister Register(Type serviceType, object implementationInstance, bool replace = true)
    {
        if (replace)
        {
            serviceRegistry.RegisterInstance(serviceType, implementationInstance);
        }
        else
        {
            serviceRegistry.RegisterInstance(serviceType, implementationInstance, GetServiceName());
        }

        return this;
    }

    /// <inheritdoc />
    public IServiceRegister TryRegister(Type serviceType, Type implementationType, Lifetime lifetime = Lifetime.Singleton, RegistrationCompareMode mode = RegistrationCompareMode.ServiceType)
    {
        if (mode == RegistrationCompareMode.ServiceType)
        {
            if (!serviceRegistry.AvailableServices.Any(r => r.ServiceType == serviceType))
                Register(serviceType, implementationType, lifetime);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            if (!serviceRegistry.AvailableServices.Any(r => r.ServiceType == serviceType && GetImplementationType(r) == implementationType))
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
        if (mode == RegistrationCompareMode.ServiceType)
        {
            if (!serviceRegistry.AvailableServices.Any(r => r.ServiceType == serviceType))
                Register(serviceType, implementationFactory, lifetime);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            Type[] typeArguments = implementationFactory.GetType().GenericTypeArguments;
            if (typeArguments.Length != 2)
                throw new InvalidOperationException("implementationFactory should be of type Func<IServiceResolver, T>");
            var implementationType = typeArguments[1];
            if (!serviceRegistry.AvailableServices.Any(r => r.ServiceType == serviceType && GetImplementationType(r) == implementationType))
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
        if (mode == RegistrationCompareMode.ServiceType)
        {
            if (!serviceRegistry.AvailableServices.Any(r => r.ServiceType == serviceType))
                Register(serviceType, implementationInstance);
        }
        else if (mode == RegistrationCompareMode.ServiceTypeAndImplementationType)
        {
            var implementationType = implementationInstance.GetType();
            if (!serviceRegistry.AvailableServices.Any(r => r.ServiceType == serviceType && GetImplementationType(r) == implementationType))
                Register(serviceType, implementationInstance);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return this;
    }

    private static ILifetime ToLifetime(Lifetime lifetime)
    {
        return lifetime switch
        {
            Lifetime.Transient => new PerRequestLifeTime(),
            Lifetime.Singleton => new PerContainerLifetime(),
            _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null)
        };
    }

    private string GetServiceName()
    {
        return "ENQ_" + Interlocked.Increment(ref counter);
    }

    private static Type GetImplementationType(ServiceRegistration registration)
    {
        if (registration.ImplementingType != null)
        {
            return registration.ImplementingType;
        }
        else if (registration.Value != null)
        {
            return registration.Value.GetType();
        }
        else if (registration.FactoryExpression != null)
        {
            Type[] typeArguments = registration.FactoryExpression.GetType().GenericTypeArguments;

            if (typeArguments.Length != 2)
                throw new InvalidOperationException("ServiceRegistration.FactoryExpression should have 2 generic type arguments");

            return typeArguments[1];
        }

        throw new InvalidOperationException("ImplementingType, Value or FactoryExpression must be non null in ServiceRegistration");
    }

    private class LightInjectResolver : IServiceResolver
    {
        private readonly IServiceFactory serviceFactory;
        private readonly IServiceRegistry serviceRegistry;

        public LightInjectResolver(IServiceFactory serviceFactory, IServiceRegistry serviceRegistry)
        {
            this.serviceFactory = serviceFactory;
            this.serviceRegistry = serviceRegistry;
        }

        public TService Resolve<TService>() where TService : class
        {
            try
            {
                return serviceFactory.GetInstance<TService>();
            }
            catch (InvalidOperationException e) when (e.Message.StartsWith("Unable to resolve type:") && e.Message.EndsWith("service name: "))
            {
                // fallback to resolve the last registration from all available for this service if there is no default one
                var registration = serviceRegistry.AvailableServices
                    .Where(r => r.ServiceType == typeof(TService) && r.ServiceName.StartsWith("ENQ_"))
                    .OrderByDescending(r => r.ServiceName, RegistrationsComparer.Instance)
                    .FirstOrDefault();

                if (registration == null)
                    throw;

                return serviceFactory.GetInstance<TService>(registration.ServiceName);
            }
        }

        public IServiceResolverScope CreateScope()
        {
            return new ServiceResolverScope(this);
        }

        private sealed class RegistrationsComparer : IComparer<string>
        {
            public static readonly RegistrationsComparer Instance = new();

            public int Compare(string x, string y)
            {
                return long.Parse(x.Substring(4)).CompareTo(long.Parse(y.Substring(4)));
            }
        }
    }
}
