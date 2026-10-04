namespace EasyNetQ;

public static class Argument
{
    # region Queue

    public const string QueueType = "x-queue-type";
    public const string QueueMode = "x-queue-mode";
    public const string Expires = "x-expires";
    public const string MaxPriority = "x-max-priority";
    public const string MaxLength = "x-max-length";
    public const string MaxLengthBytes = "x-max-length-bytes";
    public const string SingleActiveConsumer = "x-single-active-consumer";
    public const string DeadLetterExchange = "x-dead-letter-exchange";
    public const string DeadLetterRoutingKey = "x-dead-letter-routing-key";
    public const string MessageTtl = "x-message-ttl";
    public const string QueueMasterLocator = "x-queue-master-locator";
    public const string DeadLetterStrategy = "x-dead-letter-strategy";
    public const string Overflow = "x-overflow";

    # endregion Queue

    #region Exchange

    public const string AlternateExchange = "alternate-exchange";
    public const string DelayedType = "x-delayed-type";

    #endregion
}
