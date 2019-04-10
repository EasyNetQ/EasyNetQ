using System.Collections.Generic;

namespace EasyNetQ
{
    public interface IExchangeDeclareConfiguration
    {
        /// <summary>
        /// Sets as durable or not. Durable exchanges remain active when a server restarts.
        /// </summary>
        /// <param name="isDurable">The durable flag to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IExchangeDeclareConfiguration AsDurable(bool isDurable);

        /// <summary>
        /// Sets as autoDelete or not. If set, the exchange is deleted when all queues have finished using it.
        /// </summary>
        /// <param name="isAutoDelete">The autoDelete flag to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IExchangeDeclareConfiguration AsAutoDelete(bool isAutoDelete);

        /// <summary>
        /// Sets type of the exchange.
        /// </summary>
        /// <param name="type">The type to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IExchangeDeclareConfiguration WithType(string type);

        /// <summary>
        /// Sets an argument for exchange declaration
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

        public IExchangeDeclareConfiguration AsDurable(bool isDurable)
        {
            IsDurable = isDurable;
            return this;
        }

        public IExchangeDeclareConfiguration AsAutoDelete(bool isAutoDelete)
        {
            IsAutoDelete = isAutoDelete;
            return this;
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
