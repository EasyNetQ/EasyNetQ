namespace EasyNetQ.Tests
{
    public class TestMessage
    {
    }

    [Queue("MyQueue", QueueType = QueueType.Quorum)]
    public class QuorumQueueTestMessage
    {
    }

    [Queue("MyQueue", QueueType = QueueType.Quorum)]
    public interface IQuorumQueueTestMessage
    {
    }

    [Queue("MyQueue", ExchangeName = "MyExchange")]
    public class AnnotatedTestMessage
    {
    }

    [Queue("MyQueue", ExchangeName = "MyExchange")]
    public interface IAnnotatedTestMessage
    {
    }

    [Queue("MyQueue")]
    public class QueueNameOnlyAnnotatedTestMessage
    {
    }

    [Queue("MyQueue")]
    public interface IQueueNameOnlyAnnotatedTestMessage
    {
    }

    [Queue("")]
    public class EmptyQueueNameAnnotatedTestMessage
    {
    }

    [Queue("")]
    public interface IEmptyQueueNameAnnotatedTestMessage
    {
    }

    public class MyMessage
    {
        public string Text { get; set; }
    }

    public class MyOtherMessage
    {
        public string Text { get; set; }
    }
}
