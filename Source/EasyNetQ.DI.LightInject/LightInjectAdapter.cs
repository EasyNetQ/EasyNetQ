using System;
using LightInject;

namespace EasyNetQ.DI
{    
    public class LightInjectAdapter : IContainer, IDisposable
    {
        private readonly IServiceContainer _lightInjectContainer;

        public LightInjectAdapter(IServiceContainer lightInjectContainer)
        {
            if (lightInjectContainer == null)
            {
                throw new ArgumentNullException("lightInjectContainer");
            }

            _lightInjectContainer = lightInjectContainer;
        }

        public TService Resolve<TService>() where TService : class
        {
            return _lightInjectContainer.GetInstance<TService>();
        }

        public IServiceRegister Register<TService>(Func<IServiceProvider, TService> serviceCreator) where TService : class
        {
            if (!_lightInjectContainer.CanGetInstance(typeof(TService), string.Empty))
            {
                _lightInjectContainer.Register<TService>(ctx => serviceCreator(this), new PerContainerLifetime());
            }
            return this;
        }

        public IServiceRegister Register<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            if (!_lightInjectContainer.CanGetInstance(typeof(TService), string.Empty))
            {
                _lightInjectContainer.Register<TService, TImplementation>(new PerContainerLifetime());
            }
            return this;
        }

        public void Dispose()
        {
            // Intentionally empty
        }
    }
}
