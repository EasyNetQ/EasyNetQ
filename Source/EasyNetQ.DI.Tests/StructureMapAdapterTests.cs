using EasyNetQ.DI.StructureMap;
using StructureMap;

namespace EasyNetQ.DI.Tests
{
    public class StructureMapAdapterTests : ContainerAdapterTests<StructureMapAdapter>
    {
        public StructureMapAdapterTests()
            : base(() => new StructureMapAdapter(new Container()))
        {
        }
    }
}