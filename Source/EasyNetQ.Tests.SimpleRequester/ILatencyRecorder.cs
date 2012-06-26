using System;

namespace EasyNetQ.Tests.SimpleRequester
{
    public interface ILatencyRecorder : IDisposable
    {
        void RegisterRequest(long requestId);
        void RegisterResponse(long responseId);
    }
}