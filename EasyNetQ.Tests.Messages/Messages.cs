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
        public string Text { get; set; }
        public bool CausesExceptionInServer { get; set; }
    }

    [Serializable]
    public class TestResponseMessage
    {
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
}