using System;
using System.Text;
using System.Threading.Tasks;

namespace EasyNetQ.Rpc.FreshQueue
{
    static class RpcHelpers
    {
        public static void ExtractExceptionFromHeadersAndPropagateToTaskCompletionSource(IRpcHeaderKeys rpcHeaderKeys, SerializedMessage sm, TaskCompletionSource<SerializedMessage> tcs)
        {
            var isFaulted = false;
            var exceptionMessage = "The exception message has not been specified.";
            if (sm.Properties.HeadersPresent)
            {
                if (sm.Properties.Headers.ContainsKey(rpcHeaderKeys.IsFaultedKey))
                {
                    isFaulted = Convert.ToBoolean(sm.Properties.Headers[rpcHeaderKeys.IsFaultedKey]);
                }
                if (sm.Properties.Headers.ContainsKey(rpcHeaderKeys.ExceptionMessageKey))
                {
                    exceptionMessage = Encoding.UTF8.GetString((byte[])sm.Properties.Headers[rpcHeaderKeys.ExceptionMessageKey]);
                }
            }
            if (isFaulted)
            {
                tcs.TrySetException(new EasyNetQResponderException(exceptionMessage));
            }
            else
            {
                tcs.TrySetResult(sm);
            }
        }

        public static Task<SerializedMessage> ExtractExceptionFromHeadersAndPropagateToTask(IRpcHeaderKeys rpcHeaderKeys, SerializedMessage sm)
        {
            var tcs = new TaskCompletionSource<SerializedMessage>();
            ExtractExceptionFromHeadersAndPropagateToTaskCompletionSource(rpcHeaderKeys, sm, tcs);
            return tcs.Task;
        }

        public static Task<SerializedMessage> MaybeAddExceptionToHeaders()
        {
            
        }
    }
}