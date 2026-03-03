namespace EasyNetQ.AutoSubscribe;

[AttributeUsage(AttributeTargets.Method)]
public class AutoSubscriberConsumerAttribute : Attribute
{
    public AutoSubscriberConsumerAttribute()
    {

    }
    public AutoSubscriberConsumerAttribute(string subscriptionId)
    {
        SubscriptionId = subscriptionId;
    }
    public string SubscriptionId { get; init; }
}
