namespace EasyNetQ
{
    /// <summary>
    ///     Allows send configuration to be fluently extended without adding overloads
    ///     e.g.
    ///     x => x.WithPriority(2)
    /// </summary>
    public interface ISendConfiguration
    {
        /// <summary>
        ///     Sets a priority of the message
        /// </summary>
        /// <param name="priority">The priority to set</param>
        ISendConfiguration WithPriority(byte priority);
    }

    internal class SendConfiguration : ISendConfiguration
    {
        public byte? Priority { get; private set; }

        public ISendConfiguration WithPriority(byte priority)
        {
            Priority = priority;
            return this;
        }
    }
}
