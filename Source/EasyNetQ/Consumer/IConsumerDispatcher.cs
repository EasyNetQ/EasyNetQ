using System;

namespace EasyNetQ.Consumer
{
    public interface IConsumerDispatcher : IDisposable
    {
        /// <summary>
        /// These actions will be cleared with OnDisconnect
        /// </summary>
        /// <param name="action"></param>
        void QueueTransientAction(Action action);

        /// <summary>
        /// These actions will survive OnDisconnect
        /// </summary>
        /// <param name="action"></param>
        void QueueDurableAction(Action action);

        void OnDisconnected();
    }
}
