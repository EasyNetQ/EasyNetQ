using System;
using SimpleInjector;

namespace EasyNetQ.DI.SimpleInjector
{
    public class SimpleInjectorAdapter : IServiceRegister, IServiceResolver
    {
        private readonly Container container;

        public SimpleInjectorAdapter(Container container)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));

            container.RegisterSingleton((IServiceResolver) this);
            container.RegisterSingleton((IServiceRegister) this);
        }

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

        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            container.RegisterSingleton(instance);
            return this;
        }

        public TService Resolve<TService>() where TService : class
        {
            return container.GetInstance<TService>();
        }

        public IServiceResolverScope CreateScope()
        {
            return new ServiceResolverScope(this);
        }
    }
}