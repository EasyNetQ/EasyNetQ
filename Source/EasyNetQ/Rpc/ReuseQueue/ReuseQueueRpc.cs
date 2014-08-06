using System;
using System.Threading.Tasks;

namespace EasyNetQ.Rpc.ReuseQueue
{
    class ReuseQueueRpc : IRpc
    {
        public Task<TResponse> Request<TRequest, TResponse>(TRequest request) where TRequest : class where TResponse : class
        {
            throw new NotImplementedException();
        }

        public IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder) where TRequest : class where TResponse : class
        {
            throw new NotImplementedException();
        }
    }
}