using Castle.Windsor;
using EasyNetQ.DI.Windsor;

namespace EasyNetQ.DI.Tests
{
    public class WindsorAdapterTests : ContainerAdapterTests<WindsorAdapter>
    {
        public WindsorAdapterTests()
            : base(new WindsorAdapter(new WindsorContainer()), s => s, s => s)
        {
        }
    }
}