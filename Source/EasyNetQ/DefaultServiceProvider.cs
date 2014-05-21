using System;
using System.Collections.Generic;
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
        private readonly IDictionary<Type, object> factories = new Dictionary<Type, object>();
        private readonly IDictionary<Type, Type> registrations = new Dictionary<Type, Type>();

        private readonly IDictionary<Type, object> instances = new Dictionary<Type, object>();

        private bool ServiceIsRegistered(Type serviceType)
        {
            return factories.ContainsKey(serviceType) || registrations.ContainsKey(serviceType);
        }

        public virtual IServiceRegister Register<TService>(Func<IServiceProvider, TService> serviceCreator) where TService : class
        {
            Preconditions.CheckNotNull(serviceCreator, "serviceCreator");

            var serivceType = typeof (TService);

            // first to register wins
            if (ServiceIsRegistered(serivceType)) return this;

            factories.Add(serivceType, serviceCreator);
            return this;
        }

        public virtual TService Resolve<TService>() where TService : class
        {
            var serivceType = typeof (TService);

            if (!ServiceIsRegistered(serivceType))
            {
                throw new EasyNetQException("No service of type {0} has been registered", serivceType.Name);
            }

            if (!instances.ContainsKey(serivceType))
            {
                if (registrations.ContainsKey(serivceType))
                {
                    var implementationType = registrations[serivceType];
                    var service = CreateServiceInstance(implementationType);
                    instances.Add(serivceType, service);
                }

                if (factories.ContainsKey(serivceType))
                {
                    var service = ((Func<IServiceProvider, TService>)factories[serivceType])(this);
                    instances.Add(serivceType, service);
                }
            }

            return (TService)instances[serivceType];
        }

        public object Resolve(Type serviceType)
        {
            return typeof (DefaultServiceProvider)
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
            var serviceType = typeof (TService);
            var implementationType = typeof (TImplementation);

            //  first to register wins
            if (ServiceIsRegistered(serviceType)) return this;

            if (!serviceType.IsAssignableFrom(implementationType))
            {
                throw new EasyNetQException("Component {0} does not implement service interface {1}",
                    implementationType.Name, serviceType.Name);
            }

            var constructors = implementationType.GetConstructors();
            if (constructors.Length != 1)
            {
                throw new EasyNetQException("An EasyNetQ service must have one and one only constructor. " +
                    "Service {0} has {1}", implementationType.Name, constructors.Length.ToString());
            }

            registrations.Add(serviceType, implementationType);
            return this;
        }
    }
}