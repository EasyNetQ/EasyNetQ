using System.Reflection;

// EasyNetQ version number: <major>.<minor>.0.<build>
[assembly: AssemblyVersion("0.7.1.0")]

// 0.7.1.0  Daniel Wertheim's AutoSubscriber
// 0.7.0.0  Added IServiceProvider to make it easy to plug in your own dependencies. Some breaking changes to RabbitHutch
// 0.6.3.0  Consumer Queue now uses BCL BlockingCollection.
// 0.6.2.0  New model cleanup strategy based on consumer tracking
// 0.6.1.0  Removed InMemoryBus, Removed concrete class dependencies from FuturePublish.
// 0.6      Introduced IAdvancedBus, refactored IBus
// 0.5      Added IPublishChannel and moved Publish and Request to it from IBus
// 0.4      Topic based routing
// 0.3      Upgrade to RabbitMQ.Client 2.8.1.0
// 0.2      Upgrade to RabbitMQ.Client 2.7.0.0
// 0.1      Initial