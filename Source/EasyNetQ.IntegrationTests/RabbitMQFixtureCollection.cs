using Xunit;

namespace EasyNetQ.IntegrationTests
{
    [CollectionDefinition("RabbitMQ")]
    public class RabbitMQFixtureCollection : ICollectionFixture<RabbitMQFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
