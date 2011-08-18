using System;

namespace EasyNetQ.Scheduler.Tests
{
    public class MockRawByteBus : IRawByteBus
    {
        public Action<string, byte[]> RawPublishDelegate { get; set; }

        public void RawPublish(string typeName, byte[] messageBody)
        {
            if (RawPublishDelegate != null)
                RawPublishDelegate(typeName, messageBody);
        }
    }
}