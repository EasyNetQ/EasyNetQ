using EasyNetQ.DI.StructureMap;
using StructureMap;

namespace EasyNetQ.DI.Tests
{
    public class StructureMapAdapterTests : ContainerAdapterTests
    {
        StructureMapAdapter adapter;

        protected override IServiceRegister CreateServiceRegister()
        {
            return this.adapter = new StructureMapAdapter(new Container());
        }

        protected override IServiceResolver CreateServiceResolver()
        {
            return adapter;
        }
    }
}