﻿using System;
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
            container.Register<IServiceResolver>(this);
            container.Register<IServiceRegister>(this);
        }

        public TService Resolve<TService>() where TService : class
        {
            return container.Resolve<TService>();
        }

        public IServiceResolver CreateScope()
        {
            return this;
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
    }
}