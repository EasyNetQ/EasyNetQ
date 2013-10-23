namespace EasyNetQ.Hosepipe.Tests
{
    public static class Helper
    {
        public static MessageReceivedInfo CreateMessageRecievedInfo()
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