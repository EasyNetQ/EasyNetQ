namespace EasyNetQ.Consumer
{
    public readonly struct ConsumerExecutionContext
    {
        public MessageHandler Handler { get; }
        public MessageReceivedInfo Info { get; }
        public MessageProperties Properties { get; }
        public byte[] Body { get; }

        public ConsumerExecutionContext(
            MessageHandler handler,
            MessageReceivedInfo info,
            MessageProperties properties,
            byte[] body
        )
        {
            Preconditions.CheckNotNull(handler, "userHandler");
            Preconditions.CheckNotNull(info, "info");
            Preconditions.CheckNotNull(properties, "properties");
            Preconditions.CheckNotNull(body, "body");

            Handler = handler;
            Info = info;
            Properties = properties;
            Body = body;
        }
    }
}
