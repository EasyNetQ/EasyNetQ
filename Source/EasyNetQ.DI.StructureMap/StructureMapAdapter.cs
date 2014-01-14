using System;
using System.Reflection;
using StructureMap;

namespace EasyNetQ.DI
{
    public class StructureMapAdapter : IContainer, IDisposable
    {
        private readonly StructureMap.IContainer structureMapContainer;

        public StructureMapAdapter(StructureMap.IContainer structureMapContainer)
        {
            this.structureMapContainer = structureMapContainer;
        }

        public TService Resolve<TService>() where TService : class
        {
            return structureMapContainer.GetInstance<TService>();
        }

        public IServiceRegister Register<TService>(Func<IServiceProvider, TService> serviceCreator) where TService : class
        {
            if (ServiceRegistered<TService>()) return this;
            structureMapContainer.Configure(
                c => c.For<TService>().Singleton().Use(() => serviceCreator(this))
                );
            return this;
        }

        public IServiceRegister Register<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            if (ServiceRegistered<TService>()) return this;
            structureMapContainer.Configure(
                c => c.For<TService>().Singleton().Use(ctx => ctx.GetInstance<TImplementation>())
                );
            return this;
        }

        private bool ServiceRegistered<T>()
        {
            var instance = structureMapContainer.TryGetInstance(typeof(T));
            var d = instance as Delegate;
            if (d != null)
            {
                try
                {
                    d.DynamicInvoke();
                }
                catch (TargetInvocationException ex)
                {
                    var inner = ex.InnerException as StructureMapException;
                    if (inner == null)
                        throw;
                    if (inner.ErrorCode == 202)
                        return false;
                    throw;
                }
            }
            return instance != null;
        }

        public void Dispose()
        {
            structureMapContainer.Dispose();
        }
    }
}
