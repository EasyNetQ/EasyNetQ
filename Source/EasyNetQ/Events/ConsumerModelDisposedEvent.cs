namespace EasyNetQ.Events
{
    public struct ConsumerModelDisposedEvent
    {
        public string ConsumerTag { get; }

        public ConsumerModelDisposedEvent(string consumerTag)
        {
            ConsumerTag = consumerTag;
        }
    }
}
