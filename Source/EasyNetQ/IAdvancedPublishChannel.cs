using System;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public interface IAdvancedPublishChannel : IDisposable
    {
        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="exchange">The exchange to publish to</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="message">The message to publish</param>
        /// <param name="configure">Configure the publish</param>
        void Publish<T>(IExchange exchange, string routingKey, IMessage<T> message, Action<IAdvancedPublishConfiguration> configure) where T : class;

        /// <summary>
        /// Publish raw bytes to the bus.
        /// </summary>
        /// <param name="exchange">The exchange to publish to</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="properties">The message properties</param>
        /// <param name="messageBody">The message bytes to publish</param>
        /// <param name="configure">Configure the publish</param>
        void Publish(IExchange exchange, string routingKey, MessageProperties properties, byte[] messageBody, Action<IAdvancedPublishConfiguration> configure);

        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="exchange">The exchange to publish to</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="message">The message to publish</param>
        void Publish<T>(IExchange exchange, string routingKey, IMessage<T> message) where T : class;

        /// <summary>
        /// Publish raw bytes to the bus.
        /// </summary>
        /// <param name="exchange">The exchange to publish to</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="properties">The message properties</param>
        /// <param name="messageBody">The message bytes to publish</param>
        void Publish(IExchange exchange, string routingKey, MessageProperties properties, byte[] messageBody);
    }
}