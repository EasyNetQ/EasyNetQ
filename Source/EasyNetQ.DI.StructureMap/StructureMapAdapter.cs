using System;

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
            structureMapContainer.Configure(
                c => c.For<TService>().Singleton().Use(() => serviceCreator(this))
                );
            return this;
        }

        public IServiceRegister Register<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            structureMapContainer.Configure(
                c => c.For<TService>().Singleton().Use(ctx => ctx.GetInstance<TImplementation>())
                );
            return this;
        }

        public void Dispose()
        {
            structureMapContainer.Dispose();
        }
    }
}
