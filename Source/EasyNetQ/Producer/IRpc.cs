using System;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    /// <summary>
    /// An RPC style request-response pattern
    /// </summary>
    public interface IRpc
    {
        /// <summary>
        /// Make a request to an RPC service
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="request">The request message</param>
        /// <returns>Returns a task that yields the result when the response arrives</returns>
        Task<TResponse> Request<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Set up a responder for an RPC service.
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="responder">A function that performs the response</param>
        IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
            where TRequest : class
            where TResponse : class;
    }

    public interface IAdvancedRpc
    {
        Task<AdvancedRabbitMessage> Request(IExchange exchange,
                                            string returnQueueName,
                                            string routingKey,
                                            bool mandatory,
                                            bool immediate,
                                            AdvancedRabbitMessage msg);

        IDisposable Respond(IQueue queue, Func<AdvancedRabbitMessage, AdvancedRabbitMessage> handleRequest, Action<IConsumerConfiguration> configure);
    }

    public class AdvancedRabbitMessage
    {
        public byte[] MessageBody { get; set; }
        public MessageProperties MessageProperties { get; set; }
    }
}