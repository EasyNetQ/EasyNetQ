﻿using System;

namespace EasyNetQ.Consumer
{
    public interface IConsumerDispatcher : IDisposable
    {
        void QueueAction(Action action);
        void QueueAction(Action action, bool surviveDisconnect);
        void OnDisconnected();
    }
}
