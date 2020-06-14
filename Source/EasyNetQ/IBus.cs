using System;

namespace EasyNetQ
{
    /// <summary>
    /// Provides a simple Publish/Subscribe, Request/Response, Send/Receive and Delayed Publish API for a message bus.
    /// </summary>
    public interface IBus : IDisposable
    {
        /// <summary>
        /// Provides a simple Publish/Subscribe API
        /// </summary>
        IPubSub PubSub { get; }

        /// <summary>
        /// Provides a simple Request/Response API
        /// </summary>
        IRpc Rpc { get; }

        /// <summary>
        /// Provides a simple Send/Receive API
        /// </summary>
        ISendReceive SendReceive { get; }

        /// <summary>
        /// Provides a simple Delayed Publish API
        /// </summary>
        IScheduler Scheduler { get; }

        /// <summary>
        /// Return the advanced EasyNetQ advanced API.
        /// </summary>
        IAdvancedBus Advanced { get; }
    }
}
