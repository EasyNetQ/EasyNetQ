namespace EasyNetQ;

public static class ExchangeType
{
    public const string Direct = "direct";
    public const string Topic = "topic";
    public const string Fanout = "fanout";
    public const string Header = "headers";
    public const string DelayedMessage = "x-delayed-message";
}
