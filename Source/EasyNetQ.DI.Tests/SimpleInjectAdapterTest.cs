using EasyNetQ.DI.SimpleInjector;
using SimpleInjector;

namespace EasyNetQ.DI.Tests
{
    public class SimpleInjectAdapterTest : ContainerAdapterTest<SimpleInjectorAdapter>
    {
        public SimpleInjectAdapterTest()
            : base(() => new SimpleInjectorAdapter(new Container()))
        {
        }
    }
}