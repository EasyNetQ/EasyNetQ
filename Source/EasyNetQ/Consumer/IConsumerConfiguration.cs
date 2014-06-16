namespace EasyNetQ.Consumer
{
    public interface IConsumerConfiguration
    {
        int Priority { get; }
        bool CancelOnHaFailover { get; }

        IConsumerConfiguration WithPriority(int priority);

        IConsumerConfiguration WithCancelOnHaFailover(bool cancelOnHaFailover = true);
    }

    public class ConsumerConfiguration : IConsumerConfiguration
    {
        public ConsumerConfiguration()
        {
            Priority = 0;
            CancelOnHaFailover = false;
        }

        public int Priority { get; private set; }
        public bool CancelOnHaFailover { get; private set; }

        public IConsumerConfiguration WithPriority(int priority)
        {
            Priority = priority;
            return this;
        }

        public IConsumerConfiguration WithCancelOnHaFailover(bool cancelOnHaFailover = true)
        {
            CancelOnHaFailover = cancelOnHaFailover;
            return this;
        }
    }
}