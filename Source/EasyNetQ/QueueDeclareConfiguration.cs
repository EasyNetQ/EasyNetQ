using System.Collections.Generic;

namespace EasyNetQ
{
    /// <summary>
    /// Allows queue declaration configuration to be fluently extended without adding overloads to IBus
    ///
    /// e.g.
    /// x => x.WithMaxPriority(42)
    /// </summary>
    public interface IQueueDeclareConfiguration
    {
        /// <summary>
        /// Sets as durable or not. Durable queues remain active when a server restarts.
        /// </summary>
        /// <param name="isDurable">The durable flag to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration AsDurable(bool isDurable);

        /// <summary>
        /// Sets as exclusive or not. Exclusive queues may only be accessed by the current connection, and are deleted when that connection closes.
        /// </summary>
        /// <param name="isExclusive">The exclusive flag to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration AsExclusive(bool isExclusive);

        /// <summary>
        /// Sets as autoDelete or not. If set, the queue is deleted when all consumers have finished using it.
        /// </summary>
        /// <param name="isAutoDelete">The autoDelete flag to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration AsAutoDelete(bool isAutoDelete);

        /// <summary>
        /// Sets a raw argument for query declaration
        /// </summary>
        /// <param name="name">The argument name to set</param>
        /// <param name="value">The argument value to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration WithArgument(string name, object value);
    }

    internal class QueueDeclareConfiguration : IQueueDeclareConfiguration
    {
        public bool IsDurable { get; private set; } = true;
        public bool IsExclusive { get; private set; }
        public bool IsAutoDelete { get; private set; }

        public IDictionary<string, object> Arguments { get; private set; }

        public IQueueDeclareConfiguration AsDurable(bool isDurable)
        {
            IsDurable = isDurable;
            return this;
        }

        public IQueueDeclareConfiguration AsExclusive(bool isExclusive)
        {
            IsExclusive = isExclusive;
            return this;
        }

        public IQueueDeclareConfiguration AsAutoDelete(bool isAutoDelete)
        {
            IsAutoDelete = isAutoDelete;
            return this;
        }

        public IQueueDeclareConfiguration WithArgument(string name, object value)
        {
            (Arguments ??= new Dictionary<string, object>())[name] = value;
            return this;
        }
    }
}
