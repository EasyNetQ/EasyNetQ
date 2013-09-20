using System;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;

namespace EasyNetQ
{
    /// <summary>
    /// Provides a simple Publish/Subscribe and Request/Response API for a message bus.
    /// </summary>
    public interface IBus : IDisposable
    {
        /// <summary>
        /// Opens and returns a new IPublishChannel. Note that IPublishChannel implements IDisposable
        /// and must be disposed.
        /// </summary>
        /// <returns>An IPublishChannel</returns>
        IPublishChannel OpenPublishChannel();

        /// <summary>
        /// Opens and returns a new IPublishChannel. Note that IPublishChannel implements IDisposable
        /// and must be disposed.
        /// </summary>
        /// <param name="configure">
        /// Channel configuration e.g. x => x.WithPublisherConfirms()
        /// </param>
        /// <returns>An IPublishChannel</returns>
        IPublishChannel OpenPublishChannel(Action<IChannelConfiguration> configure);

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="onMessage">
        /// The action to run when a message arrives. When onMessage completes the message
        /// recipt is Ack'd. All onMessage delegates are processed on a single thread so you should
        /// avoid long running blocking IO operations. Consider using SubscribeAsync
        /// </param>
        void Subscribe<T>(string subscriptionId, Action<T> onMessage) where T : class;

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="onMessage">
        /// The action to run when a message arrives. When onMessage completes the message
        /// recipt is Ack'd. All onMessage delegates are processed on a single thread so you should
        /// avoid long running blocking IO operations. Consider using SubscribeAsync
        /// </param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithTopic("uk.london")
        /// </param>
        void Subscribe<T>(string subscriptionId, Action<T> onMessage, Action<ISubscriptionConfiguration<T>> configure) 
            where T : class;

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// Allows the subscriber to complete asynchronously.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="onMessage">
        /// The action to run when a message arrives. onMessage can immediately return a Task and
        /// then continue processing asynchronously. When the Task completes the message will be
        /// Ack'd.
        /// </param>
        void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage) where T : class;

        /// <summary>
        /// Subscribes to a stream of messages that match a .NET type.
        /// </summary>
        /// <typeparam name="T">The type to subscribe to</typeparam>
        /// <param name="subscriptionId">
        /// A unique identifier for the subscription. Two subscriptions with the same subscriptionId
        /// and type will get messages delivered in turn. This is useful if you want multiple subscribers
        /// to load balance a subscription in a round-robin fashion.
        /// </param>
        /// <param name="onMessage">
        /// The action to run when a message arrives. onMessage can immediately return a Task and
        /// then continue processing asynchronously. When the Task completes the message will be
        /// Ack'd.
        /// </param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithTopic("uk.london").WithArgument("x-message-ttl", "60")
        /// </param>
        void SubscribeAsync<T>(string subscriptionId, Func<T, Task> onMessage, Action<ISubscriptionConfiguration<T>> configure) 
            where T : class;

        /// <summary>
        /// Responds to an RPC request.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="responder">
        /// A function to run when the request is received. It should return the response.
        /// </param>
        void Respond<TRequest, TResponse>(Func<TRequest, TResponse> responder) 
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Responds to an RPC request asynchronously.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="responder">
        /// A function to run when the request is received.
        /// </param>
        void RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder) 
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Fires once the bus has connected to a RabbitMQ server.
        /// </summary>
        event Action Connected;

        /// <summary>
        /// Fires when the bus disconnects from a RabbitMQ server.
        /// </summary>
        event Action Disconnected;

        /// <summary>
        /// True if the bus is connected, False if it is not.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Return the advanced EasyNetQ advanced API.
        /// </summary>
        IAdvancedBus Advanced { get; }
    }
}