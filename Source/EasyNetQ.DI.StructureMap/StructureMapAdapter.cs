using System;

namespace EasyNetQ.DI
{
    public class StructureMapAdapter : IContainer, IDisposable
    {
        private readonly StructureMap.IContainer _structureMapContainer;

        public StructureMapAdapter(StructureMap.IContainer structureMapContainer)
        {
            _structureMapContainer = structureMapContainer;
        }

        public TService Resolve<TService>() where TService : class
        {
            return _structureMapContainer.GetInstance<TService>();
        }

        public IServiceRegister Register<TService>(Func<IServiceProvider, TService> serviceCreator) where TService : class
        {
            _structureMapContainer.Configure(
                c => c.For<TService>().Singleton().Use(() => serviceCreator(this))
                );
            return this;
        }

        public IServiceRegister Register<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            _structureMapContainer.Configure(
                c => c.For<TService>().Singleton().Use(ctx => ctx.GetInstance<TImplementation>())
                );
            return this;
        }

        public void Dispose()
        {
            _structureMapContainer.Dispose();
        }
    }
}
