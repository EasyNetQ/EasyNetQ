using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.Rpc.FreshQueue;

namespace EasyNetQ.Rpc
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

        public static Func<Task<SerializedMessage>, UserHandlerInfo> MaybeAddExceptionToHeaders(IRpcHeaderKeys rpcHeaderKeys, SerializedMessage requestMessage)
        {
            return task =>
            {
                if (task.IsFaulted)
                {
                    if (task.Exception != null)
                    {
                        var errorStackTrace = string.Join("\n\n", task.Exception.InnerExceptions.Select(e => e.StackTrace));

                        var sm = new SerializedMessage(new MessageProperties(), new byte[] { });
                        sm.Properties.Headers.Add(rpcHeaderKeys.IsFaultedKey, true);
                        sm.Properties.Headers.Add(rpcHeaderKeys.ExceptionMessageKey, errorStackTrace);
                        sm.Properties.CorrelationId = requestMessage.Properties.CorrelationId;

                        return new UserHandlerInfo(sm,task.Exception);
                    }
                }
                return new UserHandlerInfo(task.Result);
            };
        }
    }
}