using System;
using EasyNetQ.TinyIoC;

namespace EasyNetQ.DI
{
    /// <summary>
    /// Minimum IoC container inspired by
    /// http://ayende.com/blog/2886/building-an-ioc-container-in-15-lines-of-code
    /// 
    /// Note all components are singletons. Only one instance of each will be created.
    /// </summary>
    public class DefaultServiceContainer : IServiceResolver, IServiceRegister
    {
        private readonly TinyIoCContainer container = new TinyIoCContainer();

        public DefaultServiceContainer()
        {
            container.Register<IServiceResolver>(this).AsSingleton();
            container.Register<IServiceRegister>(this).AsSingleton();
        }

        public TService Resolve<TService>() where TService : class
        {
            return container.Resolve<TService>();
        }

        public IServiceRegister Register<TService>(Func<IServiceResolver, TService> serviceCreator, Lifetime lifetime = Lifetime.Singleton) where TService : class
        {
            if (container.CanResolve<TService>())
            {
                return this;
            }

            switch (lifetime)
            {
                case Lifetime.Transient:
                    container.Register((c, o) => serviceCreator(this)).AsMultiInstance();
                    break;
                case Lifetime.Singleton:
                    container.Register((c, o) => serviceCreator(this)).AsSingleton();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }

            return this;
        }

        public IServiceRegister Register<TService, TImplementation>(Lifetime lifetime = Lifetime.Singleton) where TService : class where TImplementation : class, TService
        {
            if (container.CanResolve<TService>())
            {
                return this;
            }

            switch (lifetime)
            {
                case Lifetime.Transient:
                    container.Register<TService, TImplementation>().AsMultiInstance();
                    break;
                case Lifetime.Singleton:
                    container.Register<TService, TImplementation>().AsSingleton();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
            
            return this;
        }
    }
}