using System;
using EasyNetQ.FluentConfiguration;

namespace EasyNetQ
{
    public static class BusExtensions
    {
        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="message">The message to publish</param>
        public static void Publish<T>(this IBus bus, T message) where T : class
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.PublishAsync(message)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="message">The message to publish</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithTopic("*.brighton").WithPriority(2)
        /// </param>
        public static void Publish<T>(this IBus bus, T message, Action<IPublishConfiguration> configure) where T : class
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.PublishAsync(message, configure)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Publishes a message with a topic
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="message">The message to publish</param>
        /// <param name="topic">The topic string</param>
        public static void Publish<T>(this IBus bus, T message, string topic) where T : class
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.PublishAsync(message, topic)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Makes an RPC style request
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="bus">The bus instance.</param>
        /// <param name="request">The request message.</param>
        /// <returns>The response</returns>
        public static TResponse Request<TRequest, TResponse>(this IBus bus, TRequest request)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.RequestAsync<TRequest, TResponse>(request)
                      .GetAwaiter()
                      .GetResult();
        }


        /// <summary>
        /// Makes an RPC style request
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="request">The request message</param>
        /// <param name="configure">The request configuration</param>
        /// <returns>The response</returns>
        public static TResponse Request<TRequest, TResponse>(this IBus bus, TRequest request, Action<IRequestConfiguration> configure)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.RequestAsync<TRequest, TResponse>(request, configure)
                      .GetAwaiter()
                      .GetResult();
        }


        /// <summary>
        /// Send a message directly to a queue
        /// </summary>
        /// <typeparam name="T">The type of message to send</typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to send to</param>
        /// <param name="message">The message</param>
        public static void Send<T>(this IBus bus, string queue, T message) where T : class
        {
            Preconditions.CheckNotNull(bus, "bus");
            
            bus.SendAsync(queue, message)
               .GetAwaiter()
               .GetResult();
        }
    }
}