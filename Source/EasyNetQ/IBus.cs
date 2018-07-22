using System;
using EasyNetQ.PubSub;
using EasyNetQ.Rpc;
using EasyNetQ.Scheduler;
using EasyNetQ.SendReceive;

namespace EasyNetQ
{
    /// <summary>
    /// Provides a simple Publish/Subscribe and Request/Response API for a message bus.
    /// </summary>
    public interface IBus : IDisposable
    {
        IPubSub PubSub { get; }

        IRpc Rpc { get; }

        ISendReceive SendReceive { get; }

        IScheduler Scheduler { get; }
        
        /// <summary>
        /// Return the advanced EasyNetQ advanced API.
        /// </summary>
        IAdvancedBus Advanced { get; }
    }
}