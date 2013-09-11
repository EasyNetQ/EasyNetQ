using System.Reflection;

// EasyNetQ version number: <major>.<minor>.<non-breaking-feature>.<build>
[assembly: AssemblyVersion("0.12.2.0")]

// Note: until version 1.0 expect breaking changes on 0.X versions.

// 0.12.2.0 Requested Heartbeat on by default
// 0.12.1.0 Factored declares out of AdvancedBus publish and consume.
// 0.11.1.0 New plugable validation strategy (IMessageValidationStrategy)
// 0.11.0.0 Exchange durability can be configured
// 0.10.1.0 EasyNetQ.Trace
// 0.10.0.0 John-Mark Newton's RequestAsync API change
// 0.9.2.0  C# style property names on Management.Client
// 0.9.1.0  Upgrade to RabbitMQ.Client 3.0.0.0
// 0.9.0.0  Management
// 0.8.4.0  Better client information sent to RabbitMQ
// 0.8.3.0  ConsumerErrorStrategy ack strategy
// 0.8.2.0  Publisher confirms
// 0.8.1.0  Prefetch count can be configured with the prefetchcount connection string value.
// 0.8.0.0  Fluent publisher & subscriber configuration. Breaking change to IBus and IPublishChannel.
// 0.7.2.0  Cluster support
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
