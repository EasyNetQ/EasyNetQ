using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Producer;
using Net.CommandLine;

namespace EasyNetQ.Tests.Tasks
{
    public class TestSimpleService : ICommandLineTask, IDisposable
    {
        private IBus bus;
        private const string DefaultRpcExchange = "easy_net_q_rpc";

        private static readonly Dictionary<Type, string> CustomRpcRequestConventionDictionary = new Dictionary<Type, string>
        {
            { typeof(TestModifiedRequestExhangeRequestMessage), "ChangedRpcRequestExchange" }
        };

        private static readonly Dictionary<Type, string> CustomRpcResponseConventionDictionary = new Dictionary<Type, string>
        {
            {typeof(TestModifiedResponseExhangeResponseMessage), "ChangedRpcResponseExchange" }
        };

        public Task Run(CancellationToken cancellationToken)
        {

            bus = RabbitHutch.CreateBus("host=localhost");

            bus.Advanced.Conventions.RpcRequestExchangeNamingConvention = type => CustomRpcRequestConventionDictionary.ContainsKey(type) ? CustomRpcRequestConventionDictionary[type] : DefaultRpcExchange;
            bus.Advanced.Conventions.RpcResponseExchangeNamingConvention = type => CustomRpcResponseConventionDictionary.ContainsKey(type) ? CustomRpcResponseConventionDictionary[type] : DefaultRpcExchange;

            bus.Rpc.Respond<TestModifiedRequestExhangeRequestMessage, TestModifiedRequestExhangeResponseMessage>(
                x => HandleModifiedRequestExchangeRequestAsync(x)    
            );
            bus.Rpc.Respond<TestModifiedResponseExhangeRequestMessage, TestModifiedResponseExhangeResponseMessage>(
                x => HandleModifiedResponseExchangeRequestAsync(x)
            );
            bus.Rpc.Respond<TestAsyncRequestMessage, TestAsyncResponseMessage>(x => HandleAsyncRequest(x));
            bus.Rpc.Respond<TestRequestMessage, TestResponseMessage>(x => HandleRequest(x));

            Console.WriteLine("Waiting to service requests");
            Console.WriteLine("Press enter to stop");
            Console.ReadLine();

            return Task.FromResult(0);
        }


        private static Task<TestAsyncResponseMessage> HandleAsyncRequest(TestAsyncRequestMessage request)
        {
            Console.Out.WriteLine("Got async request '{0}'", request.Text);

            return RunDelayed(1000, () =>
            {
                Console.Out.WriteLine("Sending response");
                return new TestAsyncResponseMessage { Text = request.Text + " ... completed." };
            });
        }

        public static TestResponseMessage HandleRequest(TestRequestMessage request)
        {
            // Console.WriteLine("Handling request: {0}", request.Text);
            if (request.CausesServerToTakeALongTimeToRespond)
            {
                Console.Out.WriteLine("Taking a long time to respond...");
                Thread.Sleep(5000);
                Console.Out.WriteLine("... responding");
            }
            if (request.CausesExceptionInServer)
            {
                if (request.ExceptionInServerMessage != null)
                    throw new SomeRandomException(request.ExceptionInServerMessage);
                throw new SomeRandomException("Something terrible has just happened!");
            }
            return new TestResponseMessage { Id = request.Id, Text = request.Text + " all done!" };
        }

        public static Task<TestModifiedRequestExhangeResponseMessage> HandleModifiedRequestExchangeRequestAsync(TestModifiedRequestExhangeRequestMessage request)
        {
            Console.Out.WriteLine("Responding to RPC request from exchange : "+ CustomRpcRequestConventionDictionary[typeof(TestModifiedRequestExhangeRequestMessage)]);
            return Task.FromResult(new TestModifiedRequestExhangeResponseMessage
            {
                Text = request.Text + " response!"
            });
        }

        public static Task<TestModifiedResponseExhangeResponseMessage> HandleModifiedResponseExchangeRequestAsync(TestModifiedResponseExhangeRequestMessage request)
        {
            Console.Out.WriteLine("Responding to RPC request from exchange : " + CustomRpcResponseConventionDictionary[typeof(TestModifiedResponseExhangeResponseMessage)]);
            return Task.FromResult(new TestModifiedResponseExhangeResponseMessage()
            {
                Text = request.Text + " response!"
            });
        }

        private static async Task<T> RunDelayed<T>(int millisecondsDelay, Func<T> func)
        {
            await Task.Delay(millisecondsDelay).ConfigureAwait(false);
            return func();
        }

        public void Dispose()
        {
            bus.Dispose();
            Console.WriteLine("Shut down complete");

        }
    }

    public class SomeRandomException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SomeRandomException() { }
        public SomeRandomException(string message) : base(message) { }
        public SomeRandomException(string message, Exception inner) : base(message, inner) { }
    }
}
