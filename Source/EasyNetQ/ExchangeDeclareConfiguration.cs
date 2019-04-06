using System.Collections.Generic;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public interface IExchangeDeclareConfiguration
    {
        /// <summary>
        /// Sets as durable or not. Durable queues remain active when a server restarts.
        /// </summary>
        /// <param name="durable">The durable flag to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IExchangeDeclareConfiguration AsDurable(bool durable);

        /// <summary>
        /// Sets as autoDelete or not. If set, the queue is deleted when all consumers have finished using it.
        /// </summary>
        /// <param name="autoDelete">The autoDelete flag to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IExchangeDeclareConfiguration AsAutoDelete(bool autoDelete);

        /// <summary>
        /// Sets alternate exchange of the exchange.
        /// </summary>
        /// <param name="alternateExchange">The alternate exchange to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IExchangeDeclareConfiguration WithAlternateExchange(IExchange alternateExchange);

        /// <summary>
        /// Sets type of the exchange.
        /// </summary>
        /// <param name="exchangeType">The type to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IExchangeDeclareConfiguration WithType(string exchangeType);

        /// <summary>
        /// Sets a raw argument for exchange declaration
        /// </summary>
        /// <param name="name">The argument name to set</param>
        /// <param name="value">The argument value to set</param>
        /// <returns>IExchangeDeclareConfiguration</returns>
        IExchangeDeclareConfiguration WithArgument(string name, object value);
    }

    public sealed class ExchangeDeclareConfiguration : IExchangeDeclareConfiguration
    {
        public bool IsDurable { get; private set; } = true;

        public bool IsAutoDelete { get; private set; }

        public string Type { get; private set; }

        public IDictionary<string, object> Arguments { get; } = new Dictionary<string, object>();

        public IExchangeDeclareConfiguration AsDurable(bool durable = true)
        {
            IsDurable = durable;
            return this;
        }

        public IExchangeDeclareConfiguration AsAutoDelete(bool autoDelete = true)
        {
            IsAutoDelete = autoDelete;
            return this;
        }

        public IExchangeDeclareConfiguration WithAlternateExchange(IExchange alternateExchange)
        {
            return WithArgument("alternate-exchange", alternateExchange.Name);
        }

        public IExchangeDeclareConfiguration WithType(string type)
        {
            Type = type;
            return this;
        }

        public IExchangeDeclareConfiguration WithArgument(string name, object value)
        {
            Arguments[name] = value;
            return this;
        }
    }
}
