using System;

namespace EasyNetQ.Tests
{
    [Serializable]
    public class TestRequestMessage
    {
        public string Text { get; set; }
    }

    [Serializable]
    public class TestResponseMessage
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