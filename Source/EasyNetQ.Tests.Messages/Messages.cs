using System;

namespace EasyNetQ.Tests
{
    [Serializable]
    public class TestPerformanceMessage
    {
        public string Text { get; set; }
    }

    [Serializable]
    public class TestRequestMessage
    {
        public long Id { get; set; }
        public string Text { get; set; }
        public bool CausesExceptionInServer { get; set; }
        public string ExceptionInServerMessage { get; set; }
        public bool CausesServerToTakeALongTimeToRespond { get; set; }
    }

    [Serializable]
    public class TestResponseMessage
    {
        public long Id { get; set; }
        public string Text { get; set; }
    }

    [Serializable]
    public class TestAsyncRequestMessage
    {
        public string Text { get; set; }
    }

    [Serializable]
    public class TestAsyncResponseMessage
    {
        public string Text { get; set; }
    }

    [Serializable]
    public class StartMessage
    {
        public string Text { get; set; }
    }

    [Serializable]
    public class EndMessage
    {
        public string Text { get; set; }
    }

    public interface IAnimal
    {
        string Name { get; set; }
    }

    public class Cat : IAnimal
    {
        public string Name { get; set; }
        public string Meow { get; set; }
    }

    public class Dog : IAnimal
    {
        public string Name { get; set; }
        public string Bark { get; set; }
    }
}