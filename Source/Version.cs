using System;
using System.Reflection;

// EasyNetQ version number: <major>.<minor>.<non-breaking-feature>.<build>
[assembly: AssemblyVersion("0.50.4.0")]
[assembly: CLSCompliant(true)]

// Note: until version 1.0 expect breaking changes on 0.X versions.
// 0.50.4.0 Queue max priority now uses int instead of byte.
// 0.50.3.0 Bug fix, Polymorphic request-response did not work with polymorphic response types
// 0.50.2.0 Updated RabbitMQ client to 3.5.4 and Json.NET to 7.0.1
// 0.50.1.0 Fix type in Extensions
// 0.50.0.0 Updated to RabbitMQ.Client 3.5.3
// 0.49.3.0 Polymorphic publish now works with Scheduler.Mongo
// 0.49.2.0 Fix subscription for events if queues were created in previous versions
// 0.49.1.0 Priority queue support
// 0.49.0.0 Updated to RabbitMQ.Client 3.5.1
// 0.48.1.0 Fix unhandled exception
// 0.48.0.0 Refactor IScheduler and its implementations
// 0.47.10.0 RabbitHutch.CreateBus overloads
// 0.47.9.0 TypeNameSerializer now uses a ConcurrentDictionary to store se/deserialization results.
// 0.47.8.0 Rpc.Respond will validate serialized length of TResponse upon method call to prevent silent exception when executing responder.
// 0.47.7.0 Validating ConnectionConfiguration in lowest level method of RabbitHutch.
// 0.47.6.0 AtLeastOneWithDefault -> DefaultIfEmpty
// 0.47.5.0 PersistentChannel update preventing race condition following PersistentConnection quick connection/disconnection.
// 0.47.4.0 Refactor MessageDeliveryModeStrategy
// 0.47.3.0 Using MessageDeliveryMode instead of hardcoded 1/2 and Exchange/ExchangeType update.
// 0.47.2.0 Logging is disabled by default.
// 0.47.1.0 Bug fix, when the message broker connection is lost is not possible any more publish a message on queue EasyNetQ_Default_Error_Queue.
// 0.47.0.0 It's now required to call PersistentConnection.Initialize() to bootstrap a PersistentConnection and make it start attempting to connect.
// 0.46.1.0 Fix NullReferenceException on Serialize
// 0.46.0.0 Implementation of AdvancedBusEventHandlers and events are gone from IBus.
// 0.45.0.0 IBus Subscription methods now return an ISubscriptionResult and IAdvancedBus exposes IConventions.
// 0.44.3.0 RabbitHutch.CreateBus overload
// 0.44.2.0 Bug fix, when a subscriptionId is null the queue name end with '_'
// 0.44.1.0 SSL enabled cluster support - Added SSL options per host configuration
// 0.44.0.0 Added Action<IConsumerConfiguration> overloads to Receive() on IBus, ISendReceive, and their implementations
// 0.43.1.0 Management Client fix for URI slash escaping in .NET 4.0 with https connection.
// 0.43.0.0 Use ILRepack to internally merge Newtonsoft.Json in ManagementClient, default WebRequest.KeepAlive to false to resolve spurious 'the request was aborted: the request was canceled' exceptions
// 0.42.0.0 Switched from local to UTC datetimes.
// 0.41.0.0 Dynamic removal 
// 0.40.6.0 Added parameter to set the 'x-dead-letter-routing-key' argument when declaring a queue.
// 0.40.5.0 Preconditions will check for blank argument name / exception message only when needed
// 0.40.4.0 Bug fix of Rpc
// 0.40.3.0 Upgrade to RabbitMQ.Client 3.4.3
// 0.40.2.0 ReflectionHelpers improvement
// 0.40.1.0 Fix concurrent bugs in DefaultServiceProvider
// 0.40.0.0 Exclusive Consumer
// 0.39.6.0 Fix enable recreating of exchangeTask if it is faulted
// 0.39.5.0 Fix concurrent bugs in EventBus
// 0.39.4.0 ConsumerDispatcher's dispatching thread shall not die due to single action failure.
// 0.39.3.0 SendAsync should return Task
// 0.39.2.0 Removed Immutable Packages and replaced IEventBus.cs with previous version to prevent cs1685 compiler warnings
// 0.39.1.0 Fix multiple queue's creation. Bug fix
// 0.39.0.0 Added SendAsync
// 0.38.2.0 RandomHostSelectionStrategy is default hosts selection strategy
// 0.38.1.0 Configuration of rpc timeout  
// 0.38.0.0 ILMerging to remove the potentially conflicting dependency on System.Collections.Immutable.Net40 from the NuGet
// 0.37.3.0 Remove POCO interfaces IConnectionConfiguration and IHostConfiguration
// 0.37.2.0 Upgrade to RabbitMQ.Client 3.4.0
// 0.37.1.0 AutoSubscriber Subscribe and SubscribeAsync support loading consumers from an array of types
// 0.37.0.0 Added MessageCount method to AdvancedBus
// 0.36.5.0 Make DefaultConsumerErrorStrategy thread-safe
// 0.36.4.0 Fixed EasyNetQ.nuspec by adding the dependency on System.Collections.Immutable.Net40
// 0.36.3.0 PublishedMessageEvent, DeliveredMessageEvent
// 0.36.2.0 Fixed threading issue in EventBus
// 0.36.1.0 Updated Json.Net to the latest version
// 0.36.0.0 Support for blocked connection notifications
// 0.35.5.0 Basic implementation of produce-consumer interception
// 0.35.4.0 Future publish refactor: introduced IScheduler interface.
// 0.35.3.0 Infinite timeout. (set timeout to 0)
// 0.35.2.0 Attributes caching + Exception handling around responder function, to avoid timeout on the client when the exception is thrown before the task is returned.
// 0.35.1.0 Configure request for ManagementClient
// 0.35.0.0 Use ILRepack to internally merge Newtonsoft.Json
// 0.34.0.0 basic.get added to advanced bus: IAdvancedBus.Get<T>(IQueue queue)
// 0.33.2.0 x-expires now can be configured while subscribe, using the fluent interface method x => x.WithExpires(int)
// 0.33.1.0 NinjectAdapter cannot handle first-to-register behavior, Ninject cannot handle registration of Func<>. Added ICorrelationIdGenerationStrategy, and DefaultCorrelationIdGenerationStrategy.
// 0.33.0.0 x-cancel-on-ha-failover is now false by default and can be configured with the cancelOnHaFailover connection string value and with the fluent interface method WithCancelOnHaFailover. If you set on connection string, it can't be overridden by the fluent method, instead if you leave it disabled from connection string, you can manage the behavior per consumer with the fluent interface. Possible breaking change for whom they was expecting a consumer shutdown after a cluster HA fail-over, now the consumer will be redeclared and continue to consume.
// 0.32.3.0 RabbitMQ.Client version 3.3.2
// 0.32.2.0 Updated JSON.Net to the latest version
// 0.32.1.0 Add support for message versioning
// 0.32.0.0 Handle Consumer Task Cancellation
// 0.31.1.0 Added QueueAttribute for controling queue / exchange names.
// 0.31.0.0 Added FuturePublish based on deadlettering.
// 0.30.2.0 Upgrade to RabbitMQ.Client 3.3.0
// 0.30.1.0 Added FuturePublishAsync
// 0.30.0.0 Added CancelFuturePublish functionality
// 0.29.0.0 Support returned immediate/mandatory messages
// 0.28.5.0 Added ChangeUserPassword method to the Management Client. Added the 'policymaker' to the allowed user tags.
// 0.28.4.0 Support for queue name that contains plus char (+) when using Management Client.
// 0.28.3.0 RabbitMQ.Client version 3.2.4
// 0.28.1.0 Made Send method respect the PersistentMessages configuration option
// 0.28.0.0 Consumer priority
// 0.27.5.0 Fixed PersistentChannel issue where model invalid after exception thrown. Bug fix.
// 0.27.4.0 Fixed broken non-connection string RabbitHutch.Create method
// 0.27.3.0 Can set product/platfrom info (that displays in Management UI) in connection string
// 0.27.2.0 Client information now displayed in Management UI Connections list
// 0.27.1.0 CLS-Compliant
// 0.27.0.0 RabbitMQ.Client version 3.2.1
// 0.26.7.0 Type name size checking (pending a better strategy for creating AMQP object names)
// 0.26.6.0 Better bounds checking on basic properties
// 0.26.5.0 Added non-generic publish methods
// 0.26.4.0 IConsumerErrorStrategy interface change.
// 0.26.3.0 Added persistentMessages configuration option.
// 0.26.2.0 Fixed failed reconection issue. Bug fix.
// 0.26.1.0 New policy definitions: alternate-exchange, dead-letter-exchange, dead-letter-routing-key, message-ttl, expires, max-length. Add nullability on HaMode and HaSyncMode, to let add a policy without them.
// 0.26.0.0 Request now throws exception if the responder throws on server. Requests will not timeout anymore on responder exception.
// 0.25.4.0 Exchange declare accepts alternate-exchange parameter
// 0.25.3.0 StructureMap and Windsor Container implementations
// 0.25.2.0 Can cancel Respond.
// 0.25.1.0 Autosubscriber explict interface implementation bug fix.
// 0.25.0.0 SetContainerFactory on RabbitHutch, allows replacement of EasyNetQ's internal IoC container
// 0.24.0.0 Non-Generic extension methods. Includes change to ISubscriptionConfiguration (removing generic type argument)
// 0.23.0.0 ErrorExchangeNameConvention now takes a MessageReceivedInfo argument
// 0.22.1.0 Fixed problem when executing channel actions on a non-open connection
// 0.22.0.0 Send-Receive pattern fixed.
// 0.21.0.0 Send-Receive pattern DO NOT USE THIS VERSION
// 0.20.0.0 Mutiple handlers per consumer
// 0.19.0.0 Consumer cancellation
// 0.18.1.0 JsonSerializerSettings not passed when using TypeNameSerializer
// 0.18.0.0 Publish/Subscribe polymorphism. Exchange/Queue naming conventions changed.
// 0.17.0.0 Request synchronous. Removed callback API.
// 0.16.0.0 Added DispatchAsync method to IAutoSubscriberMessageDispatcher
// 0.15.3.0 Handle multiple=true on publisher confirm ack.
// 0.15.2.0 Internal event bus
// 0.15.1.0 PublishExchangeDeclareStrategy. Only one declare now rather than once per publish.
// 0.15.0.0 Removed IPublishChannel and IAdvancedPublishChannel API. Publish now back on IBus.
// 0.14.5.0 Upgrade to RabbitMQ.Client 3.1.5
// 0.14.4.0 Consumer dispatcher queue cleared after connection lost.
// 0.14.3.0 IConsumerErrorStrategy not being disposed fix
// 0.14.2.0 MessageProperties serialization fix
// 0.14.1.0 Fixed missing properties in error message
// 0.14.0.0 Big internal consumer rewrite
// 0.13.0.0 AutoSubscriber moved to EasyNetQ.AutoSubscribe namespace.
// 0.12.4.0 Factored ConsumerDispatcher out of QueueingConsumerFactory.
// 0.12.3.0 Upgrade to RabbitMQ.Client 3.1.1
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