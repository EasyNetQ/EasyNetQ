namespace EasyNetQ.Consumer
{
    public interface IConsumerConfiguration
    {
        IConsumerConfiguration WithPriority(int priority);
        int Priority { get; }
    }

    public class ConsumerConfiguration : IConsumerConfiguration
    {
        public ConsumerConfiguration()
        {
            Priority = 0;
        }

        public int Priority { get; private set; }

        public IConsumerConfiguration WithPriority(int priority)
        {
            Priority = priority;
            return this;
        }
    }
}