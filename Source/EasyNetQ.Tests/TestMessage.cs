namespace EasyNetQ.Tests
{
    public class TestMessage
    {
         
    }

    [Queue("MyQueue", ExchangeName = "MyExchange")]
    public class AnnotatedTestMessage
    {
    }

    [Queue("MyQueue")]
    public class QueueNameOnlyAnnotatedTestMessage
    {
    }

    [Queue("")]
    public class EmptyQueueNameAnnotatedTestMessage
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