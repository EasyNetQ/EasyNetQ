using System;
using LightInject;

namespace EasyNetQ.DI.LightInject
{
    public class LightInjectAdapter : IServiceRegister
    {
        private readonly IServiceRegistry serviceRegistry;

        public LightInjectAdapter(IServiceRegistry serviceRegistry)
        {
            this.serviceRegistry = serviceRegistry ?? throw new ArgumentNullException(nameof(serviceRegistry));

            this.serviceRegistry.Register<IServiceResolver>(x => new LightInjectResolver(x), new PerRequestLifeTime());
        }

        public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
        {
            serviceRegistry.Register<TService, TImplementation>(ToLifetime(lifetime));
            return this;
        }

        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            serviceRegistry.RegisterInstance(instance);
            return this;
        }

        public IServiceRegister Register<TService>(Func<IServiceResolver, TService> factory, Lifetime lifetime = Lifetime.Singleton) where TService : class
        {
            serviceRegistry.Register(x => factory(x.GetInstance<IServiceResolver>()), ToLifetime(lifetime));
            return this;
        }

        private static ILifetime ToLifetime(Lifetime lifetime)
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    return new PerRequestLifeTime();
                case Lifetime.Singleton:
                    return new PerContainerLifetime();
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
