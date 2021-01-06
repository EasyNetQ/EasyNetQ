using Xunit;

namespace EasyNetQ.Tests
{
    public class ConnectionConfigurationExtensionsTests
    {
        [Fact]
        public void Should_fail_if_host_is_not_present()
        {
            Assert.Throws<EasyNetQException>(() =>
            {
                new ConnectionConfiguration().SetDefaultProperties();
            });
        }
    }
}
