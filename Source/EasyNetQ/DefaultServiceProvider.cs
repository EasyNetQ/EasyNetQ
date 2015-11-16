using System;
using System.Collections.Concurrent;
using System.Linq;

namespace EasyNetQ
{
    /// <summary>
    /// Minimum IoC container inspired by
    /// http://ayende.com/blog/2886/building-an-ioc-container-in-15-lines-of-code
    /// 
    /// Note all components are singletons. Only one instance of each will be created.
    /// </summary>
    public class DefaultServiceProvider : IContainer
    {
        private readonly object syncLock = new object();
        private readonly ConcurrentDictionary<Type, object> factories = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<Type, Type> registrations = new ConcurrentDictionary<Type, Type>();
        private readonly ConcurrentDictionary<Type, object> instances = new ConcurrentDictionary<Type, object>();

        private bool ServiceIsRegistered(Type serviceType)
        {
            return factories.ContainsKey(serviceType) || registrations.ContainsKey(serviceType);
        }

        public virtual IServiceRegister Register<TService>(Func<IServiceProvider, TService> serviceCreator) where TService : class
        {
            Preconditions.CheckNotNull(serviceCreator, "serviceCreator");

            lock (syncLock)
            {
                var serviceType = typeof(TService);
                if (ServiceIsRegistered(serviceType))
                    return this;
                factories.TryAdd(serviceType, serviceCreator);
                return this;
            }
        }

        public virtual TService Resolve<TService>() where TService : class
        {
            var serviceType = typeof(TService);
            object service;
            if (instances.TryGetValue(serviceType, out service))
                return (TService)service;
            lock (syncLock)
            {
                if (instances.TryGetValue(serviceType, out service))
                    return (TService)service;

                if (registrations.ContainsKey(serviceType))
                {
                    var implementationType = registrations[serviceType];
                    service = CreateServiceInstance(implementationType);
                    instances.TryAdd(serviceType, service);
                }
                else if (factories.ContainsKey(serviceType))
                {
                    service = ((Func<IServiceProvider, TService>)factories[serviceType])(this);
                    instances.TryAdd(serviceType, service);
                }
                else
                {
                    throw new EasyNetQException("No service of type {0} has been registered", serviceType.Name);
                }
                return (TService)service;
            }
        }

        public object Resolve(Type serviceType)
        {
            return typeof(DefaultServiceProvider)
                .GetMethod("Resolve", new Type[0])
                .MakeGenericMethod(serviceType)
                .Invoke(this, new object[0]);
        }

        private object CreateServiceInstance(Type implementationType)
        {
            var constructors = implementationType.GetConstructors();

            var parameters = constructors[0]
                .GetParameters()
                .Select(parameterInfo => Resolve(parameterInfo.ParameterType))
                .ToArray();

            return constructors[0].Invoke(parameters);
        }

        public IServiceRegister Register<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            lock (syncLock)
            {
                var serviceType = typeof(TService);
                var implementationType = typeof(TImplementation);

                if (ServiceIsRegistered(serviceType))
                    return this;

                if (!serviceType.IsAssignableFrom(implementationType))
                {
                    throw new EasyNetQException("Component {0} does not implement service interface {1}",
                        implementationType.Name, serviceType.Name);
                }

                var constructors = implementationType.GetConstructors();
                if (constructors.Length != 1)
                {
                    throw new EasyNetQException("An EasyNetQ service must have one and one only constructor. " +
                                                "Service {0} has {1}", implementationType.Name,
                        constructors.Length.ToString());
                }

                registrations.TryAdd(serviceType, implementationType);
                return this;
            }
        }
    }
}