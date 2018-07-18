using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;

namespace EasyNetQ
{
    public static class BusExtensions
    { 
        /// <summary>
        /// Publishes a message with a topic.
        /// When used with publisher confirms the task completes when the publish is confirmed.
        /// Task will throw an exception if the confirm is NACK'd or times out.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The bus instance</param>
        /// /// <param name="message">The message to publish</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        public static async Task PublishAsync<T>(this IBus bus, T message, CancellationToken cancellationToken = default) where T : class
        {
            Preconditions.CheckNotNull(bus, "bus");

            await bus.PublishAsync(message, c => {}, cancellationToken);
        }
        
        /// <summary>
        /// Publishes a message with a topic.
        /// When used with publisher confirms the task completes when the publish is confirmed.
        /// Task will throw an exception if the confirm is NACK'd or times out.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The bus instance</param>
        /// /// <param name="message">The message to publish</param>
        /// <param name="topic">The topic string</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        public static async Task PublishAsync<T>(this IBus bus, T message, string topic, CancellationToken cancellationToken = default) where T : class
        {
            Preconditions.CheckNotNull(bus, "bus");
            Preconditions.CheckNotNull(topic, "topic");

            await bus.PublishAsync(message, c => c.WithTopic(topic), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="message">The message to publish</param>
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Publish<T>(this IBus bus, T message, CancellationToken cancellationToken = default) where T : class
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.PublishAsync(message, cancellationToken)
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
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Publish<T>(this IBus bus, T message, Action<IPublishConfiguration> configure, CancellationToken cancellationToken = default) where T : class
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.PublishAsync(message, configure, cancellationToken)
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
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Publish<T>(this IBus bus, T message, string topic, CancellationToken cancellationToken = default) where T : class
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.PublishAsync(message, topic, cancellationToken)
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
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response</returns>
        public static TResponse Request<TRequest, TResponse>(this IBus bus, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.RequestAsync<TRequest, TResponse>(request, cancellationToken)
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
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response</returns>
        public static async Task<TResponse> RequestAsync<TRequest, TResponse>(this IBus bus, TRequest request, CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(bus, "bus");

            return await bus.RequestAsync<TRequest, TResponse>(request, c => {}, cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Makes an RPC style request
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="request">The request message</param>
        /// <param name="configure">
        /// Fluent configuration e.g. x => x.WithQueueName("uk.london")
        /// </param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The response</returns>
        public static TResponse Request<TRequest, TResponse>(this IBus bus, TRequest request, Action<IRequestConfiguration> configure, CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.RequestAsync<TRequest, TResponse>(request, configure, cancellationToken)
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
        /// <param name="cancellationToken">The cancellation token</param>
        public static void Send<T>(this IBus bus, string queue, T message, CancellationToken cancellationToken = default) where T : class
        {
            Preconditions.CheckNotNull(bus, "bus");
            
            bus.SendAsync(queue, message, cancellationToken)
               .GetAwaiter()
               .GetResult();
        }
    }
}