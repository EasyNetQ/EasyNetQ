using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EasyNetQ.Internals;

namespace EasyNetQ.AutoSubscribe;

/// <summary>
/// A consumer class, the message type it consumes and the subscription metadata from its attributes
/// (<see cref="AutoSubscriberConsumerAttribute"/>, <see cref="ForTopicAttribute"/>, <see cref="SubscriptionConfigurationAttribute"/>).
/// </summary>
public class AutoSubscriberConsumerInfo
{
    private MethodInfo? consumeMethod;

    /// <summary>
    /// Reads the metadata from the consume method's attributes by reflection.
    /// </summary>
    [RequiresUnreferencedCode(Compat.ReflectionAutoSubscriber)]
    public AutoSubscriberConsumerInfo(Type concreteType, Type interfaceType, Type messageType)
    {
        ConcreteType = concreteType;
        InterfaceType = interfaceType;
        MessageType = messageType;
        consumeMethod = concreteType.GetInterfaceMap(interfaceType).TargetMethods.Single();
        SubscriptionAttribute = consumeMethod.GetCustomAttributes(typeof(AutoSubscriberConsumerAttribute), true)
            .OfType<AutoSubscriberConsumerAttribute>()
            .SingleOrDefault();
        Topics = consumeMethod.GetCustomAttributes(typeof(ForTopicAttribute), true)
            .OfType<ForTopicAttribute>()
            .Select(a => a.Topic)
            .ToArray();
        SubscriptionConfiguration = consumeMethod.GetCustomAttributes(typeof(SubscriptionConfigurationAttribute), true)
            .OfType<SubscriptionConfigurationAttribute>()
            .FirstOrDefault()
            ?? concreteType.GetCustomAttributes(typeof(SubscriptionConfigurationAttribute), true)
                .OfType<SubscriptionConfigurationAttribute>()
                .FirstOrDefault();
    }

    /// <summary>
    /// Takes the metadata as read at compile time by the EasyNetQ source generator: no reflection.
    /// </summary>
    public AutoSubscriberConsumerInfo(
        Type concreteType,
        Type interfaceType,
        Type messageType,
        AutoSubscriberConsumerAttribute? subscriptionAttribute,
        IReadOnlyList<string> topics,
        SubscriptionConfigurationAttribute? subscriptionConfiguration
    )
    {
        ConcreteType = concreteType;
        InterfaceType = interfaceType;
        MessageType = messageType;
        SubscriptionAttribute = subscriptionAttribute;
        Topics = topics;
        SubscriptionConfiguration = subscriptionConfiguration;
    }

    /// <summary>The consumer class</summary>
    public Type ConcreteType { get; }

    /// <summary>The closed <see cref="IConsume{T}"/> or <see cref="IConsumeAsync{T}"/> it implements</summary>
    public Type InterfaceType { get; }

    /// <summary>The message type</summary>
    public Type MessageType { get; }

    /// <summary>The consume method's <see cref="AutoSubscriberConsumerAttribute"/>, if any</summary>
    public AutoSubscriberConsumerAttribute? SubscriptionAttribute { get; }

    /// <summary>The consume method's <see cref="ForTopicAttribute"/> topics; empty binds <see cref="AutoSubscriber.DefaultTopicName"/></summary>
    public IReadOnlyList<string> Topics { get; }

    /// <summary>The consume method's <see cref="SubscriptionConfigurationAttribute"/>, else the class's</summary>
    public SubscriptionConfigurationAttribute? SubscriptionConfiguration { get; }

    /// <summary>The method implementing the consume interface</summary>
    public MethodInfo ConsumeMethod
    {
        [RequiresUnreferencedCode(Compat.ReflectionAutoSubscriber)]
        get => consumeMethod ??= ConcreteType.GetInterfaceMap(InterfaceType).TargetMethods.Single();
    }
}
