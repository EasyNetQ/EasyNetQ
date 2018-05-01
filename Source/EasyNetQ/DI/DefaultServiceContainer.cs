using System;
using EasyNetQ.TinyIoC;

namespace EasyNetQ.DI
{
    public class DefaultServiceContainer : IServiceResolver, IServiceRegister
    {
        private readonly TinyIoCContainer container = new TinyIoCContainer();

        public DefaultServiceContainer()
        {
            container.Register<IServiceResolver>(this);
            container.Register<IServiceRegister>(this);
        }

        public TService Resolve<TService>() where TService : class
        {
            return container.Resolve<TService>();
        }

        public IServiceResolverScope CreateScope()
        {
            return new ServiceResolverScope(this);
        }

        public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
        {
            switch (lifetime)
            {
                case Lifetime.Transient:
                    container.Register<TService, TImplementation>().AsMultiInstance();
                    return this;
                case Lifetime.Singleton:
                    container.Register<TService, TImplementation>().AsSingleton();
                    return this;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
        }

        public IServiceRegister Register<TService>(TService instance) where TService : class
        {
            container.Register(instance);
            return this;
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}