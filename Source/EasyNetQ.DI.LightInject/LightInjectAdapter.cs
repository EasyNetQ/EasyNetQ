using System;
using LightInject;

namespace EasyNetQ.DI.LightInject
{    
    public class LightInjectAdapter : IServiceResolver, IServiceRegister
    {
        private readonly IServiceContainer container;

        public LightInjectAdapter(IServiceContainer container)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
            container.RegisterInstance((IServiceResolver)this);
            container.RegisterInstance((IServiceRegister)this);
        }

        public TService Resolve<TService>() where TService : class
        {
            return container.GetInstance<TService>();
        }

        public IServiceResolver CreateScope()
        {
            return this; // TODO: 
        }

        public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    container.Register<TService, TImplementation>(new PerRequestLifeTime());
                    return this;
                case Lifetime.Singleton:
                    container.Register<TService, TImplementation>(new PerContainerLifetime());
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
        }

        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            container.RegisterInstance(instance);
            return this;
        }
    }
}
