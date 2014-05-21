using EasyNetQ.Consumer;

namespace EasyNetQ.Events
{
    public class AckEvent
    {
        public ConsumerExecutionContext ConsumerExecutionContext { get; private set; }
        public AckResult AckResult { get; private set; }

        public AckEvent(ConsumerExecutionContext consumerExecutionContext, AckResult ackResult)
        {
            ConsumerExecutionContext = consumerExecutionContext;
            AckResult = ackResult;
        }
    }

    public enum AckResult
    {
        Ack,
        Nack,
        Exception,
        Nothing
    }
}