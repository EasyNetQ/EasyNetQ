namespace EasyNetQ.Consumer
{
    public interface IConsumerConfiguration
    {
        /// <summary>
        /// Gets the consumer's priority
        /// </summary>
        /// <returns></returns>
        int Priority { get; }

        /// <summary>
        /// Is the consumer exlusive or not
        /// </summary>
        /// <returns></returns>
        bool IsExclusive { get; }

        /// <summary>
        /// Configures the consumer's priority
        /// </summary>
        /// <returns></returns>
        IConsumerConfiguration WithPriority(int priority);
        /// <summary>
        /// Configures is consumer exclusive or not
        /// </summary>
        /// <returns></returns>
        IConsumerConfiguration AsExclusive();
    }

    public class ConsumerConfiguration : IConsumerConfiguration
    {
        public ConsumerConfiguration()
        {
            Priority = 0;
            IsExclusive = false;
        }

        public bool IsExclusive { get; private set; }
        public int Priority { get; private set; }

        public IConsumerConfiguration AsExclusive()
        {
            IsExclusive = true;
            return this;
        }

        public IConsumerConfiguration WithPriority(int priority)
        {
            Priority = priority;
            return this;
        }
    }
}