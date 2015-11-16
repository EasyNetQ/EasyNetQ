namespace EasyNetQ.Topology
{
    /// <summary>
    /// Represents an AMQP queue
    /// </summary>
    public interface IQueue : IBindable
    {
        /// <summary>
        /// The name of the queue
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Is this queue transient?
        /// </summary>
        bool IsExclusive { get; }
    }
}