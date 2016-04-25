﻿using System;

namespace EasyNetQ.Consumer
{
    public interface IConsumerConfiguration
    {
        int Priority { get; }
        bool CancelOnHaFailover { get; }
        ushort PrefetchCount { get; }
        bool IsExclusive { get; }
        Action RecoveryAction { get; }

        IConsumerConfiguration WithPriority(int priority);
        IConsumerConfiguration WithCancelOnHaFailover(bool cancelOnHaFailover = true);
        IConsumerConfiguration WithPrefetchCount(ushort prefetchCount);
        IConsumerConfiguration AsExclusive();
        IConsumerConfiguration WithRecoveryAction(Action recoveryAction);
    }

    public class ConsumerConfiguration : IConsumerConfiguration
    {
        public ConsumerConfiguration(ushort defaultPrefetchCount)
        {
            Priority = 0;
            CancelOnHaFailover = false;
            PrefetchCount = defaultPrefetchCount;
            IsExclusive = false;
        }

        public int Priority { get; private set; }
        public bool IsExclusive { get; private set; }
        public bool CancelOnHaFailover { get; private set; }
        public ushort PrefetchCount { get; private set; }
        public Action RecoveryAction { get; private set; }

        public IConsumerConfiguration WithPriority(int priority)
        {
            Priority = priority;
            return this;
        }

        public IConsumerConfiguration WithCancelOnHaFailover(bool cancelOnHaFailover = true)
        {
            CancelOnHaFailover = cancelOnHaFailover;
            return this;
        }

        public IConsumerConfiguration WithPrefetchCount(ushort prefetchCount)
        {
            PrefetchCount = prefetchCount;
            return this;
        }

        public IConsumerConfiguration AsExclusive()
        {
            IsExclusive = true;
            return this;
        }

        public IConsumerConfiguration WithRecoveryAction(Action recoveryAction)
        {
            RecoveryAction = recoveryAction;
            return this;
        }
    }
}