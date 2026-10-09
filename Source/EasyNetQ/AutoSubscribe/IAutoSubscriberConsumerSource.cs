namespace EasyNetQ.AutoSubscribe;

/// <summary>
/// The consumers of one assembly. The EasyNetQ source generator registers one per assembly that implements
/// <see cref="IConsume{T}"/> or <see cref="IConsumeAsync{T}"/>; <c>AutoSubscribe(...)</c> subscribes them all.
/// </summary>
public interface IAutoSubscriberConsumerSource
{
    /// <summary>The consumers</summary>
    IReadOnlyList<AutoSubscriberConsumer> Consumers { get; }
}
