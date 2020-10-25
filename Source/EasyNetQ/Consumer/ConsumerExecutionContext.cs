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
            Preconditions.CheckNotNull(handler, nameof(handler));
            Preconditions.CheckNotNull(receivedInfo, nameof(receivedInfo));
            Preconditions.CheckNotNull(properties, nameof(properties));
            Preconditions.CheckNotNull(body, nameof(body));

            Handler = handler;
            ReceivedInfo = receivedInfo;
            Properties = properties;
            Body = body;
        }
    }
}
