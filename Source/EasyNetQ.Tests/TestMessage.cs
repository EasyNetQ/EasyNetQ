namespace EasyNetQ.Tests;

public class TestMessage
{
}

[Queue(Name="MyQueue", Type = QueueType.Quorum)]
public class QuorumQueueTestMessage
{
}

[Queue(Name="MyQueue", Type = QueueType.Quorum)]
public interface IQuorumQueueTestMessage
{
}

[Queue(Name="MyQueue")]
[Exchange(Name="MyExchange")]
public class AnnotatedTestMessage
{
}

[Exchange(Name="MyExchange")]
[Queue(Name="MyQueue")]
public interface IAnnotatedTestMessage
{
}

[Queue(Name="MyQueue")]
public class QueueNameOnlyAnnotatedTestMessage
{
}

[Queue(Name="MyQueue")]
public interface IQueueNameOnlyAnnotatedTestMessage
{
}

[Queue]
public class EmptyQueueNameAnnotatedTestMessage
{
}

[Queue]
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
