using System;

namespace EasyNetQ.Rpc.FreshQueue
{
    class UserHandlerInfo
    {
        public SerializedMessage Response { get; private set; }
        public Exception Exception { get; private set; }


        public UserHandlerInfo(SerializedMessage response)
        {
            Response = response;
        }

        public UserHandlerInfo(SerializedMessage response, Exception exception)
        {
            Response = response;
            Exception = exception;
        }

        public bool IsFailed()
        {
            return Exception != null;
        }
    }
}