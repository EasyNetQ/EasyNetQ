using System;

namespace EasyNetQ;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class QueueAttribute : Attribute
{
    internal static readonly QueueAttribute Default = new();

    public QueueAttribute()
    {

    }

    public QueueAttribute(string name)
    {
        QueueName = name ?? string.Empty;
    }

    public string QueueName { get; set; }

    public string ExchangeName { get; set; }

    public string QueueType { get; set; }
}
