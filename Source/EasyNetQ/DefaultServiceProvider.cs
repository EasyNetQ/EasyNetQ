using System;
using System.Collections.Generic;

namespace EasyNetQ
{
    /// <summary>
    /// Minimum IoC container inspired by
    /// http://ayende.com/blog/2886/building-an-ioc-container-in-15-lines-of-code
    /// 
    /// Note all components are singletons. Only one instance of each will be created.
    /// </summary>
    public class DefaultServiceProvider : IServiceProvider, IServiceRegister
    {
        private readonly IDictionary<Type, object> components = new Dictionary<Type, object>();
        private readonly IDictionary<Type, object> instances = new Dictionary<Type, object>(); 

        public virtual TService Resolve<TService>() where TService : class 
        {
            if (!components.ContainsKey(typeof (TService)))
            {
                throw new EasyNetQException("No serviceCreator of type {0} has been registered", typeof(TService).Name);
            }

            if (!instances.ContainsKey(typeof (TService)))
            {
                var service = ((Func<IServiceProvider, TService>)components[typeof(TService)])(this);
                instances.Add(typeof(TService), service);
            }

            return (TService) instances[typeof (TService)];
        }

        public virtual IServiceRegister Register<TService>(Func<IServiceProvider, TService> serviceCreator) where TService : class
        {
            Preconditions.CheckNotNull(serviceCreator, "serviceCreator");

            // first to register wins
            if (components.ContainsKey(typeof(TService))) return this;

            components.Add(typeof(TService), serviceCreator);
            return this;
        }
    }
}