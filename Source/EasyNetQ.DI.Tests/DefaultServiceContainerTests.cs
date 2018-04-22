namespace EasyNetQ.DI.Tests
{
    public class DefaultServiceContainerTests : ContainerAdapterTests<DefaultServiceContainer>
    {
        public DefaultServiceContainerTests()
            : base(() => new DefaultServiceContainer())
        {
        }
    }
}