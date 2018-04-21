using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;

namespace EasyNetQ.DI.Windsor
{
    public class WindsorAdapter : IServiceRegister, IServiceResolver
    {
        private readonly IWindsorContainer container;

        public WindsorAdapter(IWindsorContainer container)
        {
            this.container = container ?? throw new ArgumentNullException(nameof(container));
            
            this.container.Register(Component.For<IServiceRegister>().Instance(this).LifestyleTransient());
            this.container.Register(Component.For<IServiceResolver>().Instance(this).LifestyleTransient());
        }

        public TService Resolve<TService>() where TService : class
        {
            return container.Resolve<TService>();
        }

        public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    container.Register(Component.For<TService>().ImplementedBy<TImplementation>().LifestyleTransient());
                    return this;
                case Lifetime.Singleton:
                    container.Register(Component.For<TService>().ImplementedBy<TImplementation>().LifestyleSingleton());
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
        }

        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            container.Register(Component.For<TService>().Instance(instance).LifestyleSingleton());
            return this;
        }
    }
}