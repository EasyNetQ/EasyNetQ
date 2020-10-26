namespace EasyNetQ.Consumer
{
    public readonly struct ConsumerExecutionContext
    {
        public MessageHandler Handler { get; }
        public MessageReceivedInfo ReceivedInfo { get; }
        public MessageProperties Properties { get; }
        public byte[] Body { get; }

        public ConsumerExecutionContext(
            MessageHandler handler,
            MessageReceivedInfo receivedInfo,
            MessageProperties properties,
            byte[] body
        )
        {
            Preconditions.CheckNotNull(handler, "userHandler");
            Preconditions.CheckNotNull(receivedInfo, "info");
            Preconditions.CheckNotNull(properties, "properties");
            Preconditions.CheckNotNull(body, "body");

            Handler = handler;
            ReceivedInfo = receivedInfo;
            Properties = properties;
            Body = body;
        }
    }
}
