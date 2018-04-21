namespace EasyNetQ.DI.Tests
{
    public class DefaultServiceContainerTest : ContainerAdapterTest<DefaultServiceContainer>
    {
        public DefaultServiceContainerTest()
            : base(() => new DefaultServiceContainer())
        {
        }
    }
}