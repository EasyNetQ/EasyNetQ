using System;

namespace EasyNetQ
{
    /// <summary>
    /// Simple validator only checks the basic properties Type field to see if it matches
    /// the type that was expected.
    /// </summary>
    public class DefaultMessageValidationStrategy : IMessageValidationStrategy
    {
        private readonly IEasyNetQLogger logger;
        private readonly SerializeType serializeType;

        public DefaultMessageValidationStrategy(IEasyNetQLogger logger, SerializeType serializeType)
        {
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(serializeType, "serializeType");

            this.logger = logger;
            this.serializeType = serializeType;
        }

        public void CheckMessageType<TMessage>(
            Byte[] body, 
            MessageProperties properties, 
            MessageReceivedInfo messageReceivedInfo)
        {
            Preconditions.CheckNotNull(body, "body");
            Preconditions.CheckNotNull(properties, "properties");
            Preconditions.CheckNotNull(messageReceivedInfo, "messageReceivedInfo");

            var typeName = serializeType(typeof(TMessage));
            if (properties.Type != typeName)
            {
                logger.ErrorWrite("Message type is incorrect. Expected '{0}', but was '{1}'",
                                  typeName, properties.Type);

                throw new EasyNetQInvalidMessageTypeException("Message type is incorrect. Expected '{0}', but was '{1}'",
                                                              typeName, properties.Type);
            }
        }
    }
}