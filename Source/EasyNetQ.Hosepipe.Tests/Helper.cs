namespace EasyNetQ.Hosepipe.Tests
{
    public static class Helper
    {
        public static MessageReceivedInfo CreateMessageReceivedInfo()
        {
            return new MessageReceivedInfo(
                "consumer_tag",
                0,
                false,
                "exchange_name",
                "routing_key",
                "queue_name"
            );
        }
    }
}
