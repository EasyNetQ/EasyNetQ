namespace EasyNetQ.Events
{
    public class ConsumerModelDisposedEvent
    {
        public string ConsumerTag { get; }

        public ConsumerModelDisposedEvent(string consumerTag)
        {
            ConsumerTag = consumerTag;
        }
    }
}