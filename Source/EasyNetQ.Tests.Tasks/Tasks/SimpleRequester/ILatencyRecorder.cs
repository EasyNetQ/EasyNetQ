using System;

namespace EasyNetQ.Tests.Tasks.SimpleRequester
{
    public interface ILatencyRecorder : IDisposable
    {
        void RegisterRequest(long requestId);
        void RegisterResponse(long responseId);
    }
}