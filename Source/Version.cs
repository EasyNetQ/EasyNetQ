﻿using System.Reflection;

// EasyNetQ version number: <major>.<minor>.<non-breaking-feature>.<build>
[assembly: AssemblyVersion("0.26.0.0")]

// Note: until version 1.0 expect breaking changes on 0.X versions.

// 0.26.0.0 Request now throws exception if the responder throws on server. Requests will not timeout anymore on responder exception.
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
