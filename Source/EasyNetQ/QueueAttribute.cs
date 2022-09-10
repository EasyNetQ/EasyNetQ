using System;

namespace EasyNetQ;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class QueueAttribute : Attribute
{
    internal static readonly QueueAttribute Default = new();

    public QueueAttribute(string? queueName = null)
    {
        QueueName = queueName;
    }

    public string? QueueName { get; }

    public string? ExchangeName { get; set; }

    public string? QueueType { get; set; }
}
