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
        private readonly ITypeNameSerializer typeNameSerializer;

        public DefaultMessageValidationStrategy(IEasyNetQLogger logger, ITypeNameSerializer typeNameSerializer)
        {
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");

            this.logger = logger;
            this.typeNameSerializer = typeNameSerializer;
        }

        public void CheckMessageType<TMessage>(
            Byte[] body, 
            MessageProperties properties, 
            MessageReceivedInfo messageReceivedInfo)
        {
            Preconditions.CheckNotNull(body, "body");
            Preconditions.CheckNotNull(properties, "properties");
            Preconditions.CheckNotNull(messageReceivedInfo, "messageReceivedInfo");

            Type messageType = null;
            var typeName = typeNameSerializer.Serialize(typeof(TMessage));

            try
            {
                messageType = typeNameSerializer.DeSerialize(properties.Type);
            }
            catch (EasyNetQException easyNetQException)
            {
                logger.ErrorWrite(easyNetQException.Message);

                throw new EasyNetQInvalidMessageTypeException(easyNetQException.Message);
            }

            var consumeType = typeof (TMessage);

            if (!consumeType.IsAssignableFrom(messageType))
            {
                logger.ErrorWrite("Message type is incorrect. Expected '{0}', but was '{1}'",
                                  typeName, properties.Type);

                throw new EasyNetQInvalidMessageTypeException("Message type is incorrect. Expected '{0}', but was '{1}'",
                                                              typeName, properties.Type);
            }
        }
    }
}