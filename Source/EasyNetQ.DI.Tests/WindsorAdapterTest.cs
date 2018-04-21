using Castle.Windsor;
using EasyNetQ.DI.Windsor;

namespace EasyNetQ.DI.Tests
{
    public class WindsorAdapterTest : ContainerAdapterTest<WindsorAdapter>
    {
        public WindsorAdapterTest()
            : base(() => new WindsorAdapter(new WindsorContainer()))
        {
        }
    }
}