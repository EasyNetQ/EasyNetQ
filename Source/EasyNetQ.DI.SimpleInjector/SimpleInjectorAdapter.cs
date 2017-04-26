using System;
using System.Linq;
using SimpleInjector;

namespace EasyNetQ.DI
{
    public class SimpleInjectorAdapter : IContainer, IDisposable
    {
        private readonly Container _simpleInjectorContainer;

        public SimpleInjectorAdapter(Container simpleInjectorContainer)
        {
            _simpleInjectorContainer = simpleInjectorContainer;
        }

        public TService Resolve<TService>() where TService : class
        {
            return _simpleInjectorContainer.GetInstance<TService>();
        }

        public IServiceRegister Register<TService>(Func<IServiceProvider, TService> serviceCreator) where TService : class
        {
            if (!IsRegistered<TService>())
            {
                _simpleInjectorContainer.RegisterSingleton(() => serviceCreator(this));
            }
            
            return this;
        }

        public IServiceRegister Register<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            if (!IsRegistered<TService>())
            {
                _simpleInjectorContainer.RegisterSingleton<TService, TImplementation>();
            }
            return this;
        }

        private bool IsRegistered<TService>()
        {
            var type = typeof(TService);
            return _simpleInjectorContainer.GetCurrentRegistrations().Any(r => r.ServiceType == type);
        }

        public void Dispose()
        {
            _simpleInjectorContainer.Dispose();
        }
    }
}
