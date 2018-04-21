using EasyNetQ.DI.StructureMap;
using StructureMap;

namespace EasyNetQ.DI.Tests
{
    public class StructureMapAdapterTest : ContainerAdapterTest<StructureMapAdapter>
    {
        public StructureMapAdapterTest()
            : base(() => new StructureMapAdapter(new Container()))
        {
        }
    }
}