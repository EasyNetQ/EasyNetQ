using System;
using LightInject;

namespace EasyNetQ.DI.LightInject
{
    public class LightInjectAdapter : IServiceRegister
    {
        private readonly IServiceRegistry serviceRegistry;

        public LightInjectAdapter(IServiceRegistry  serviceRegistry)
        {
            this.serviceRegistry = serviceRegistry ?? throw new ArgumentNullException(nameof(serviceRegistry));

            serviceRegistry.Register<IServiceResolver>(x => new LightInjectResolver(x), new PerRequestLifeTime());
        }

        public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    serviceRegistry.Register<TService, TImplementation>(new PerRequestLifeTime());
                    return this;
                case Lifetime.Singleton:
                    serviceRegistry.Register<TService, TImplementation>(new PerContainerLifetime());
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
        }

        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            serviceRegistry.RegisterInstance(instance);
            return this;
        }

        public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    serviceRegistry.Register(x => factory(x.GetInstance<IServiceResolver>()), new PerRequestLifeTime());
                    return this;
                case Lifetime.Singleton:
                    serviceRegistry.Register(x => factory(x.GetInstance<IServiceResolver>()), new PerContainerLifetime());
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
        }
        
        private class LightInjectResolver : IServiceResolver
        {
            private readonly IServiceFactory serviceFactory;

            public LightInjectResolver(IServiceFactory serviceFactory)
            {
                this.serviceFactory = serviceFactory;
            }

            public TService Resolve<TService>() where TService : class
            {
                return serviceFactory.GetInstance<TService>();
            }

            public IServiceResolverScope CreateScope()
            {
                return new ServiceResolverScope(this);
            }
        }

    }
}
