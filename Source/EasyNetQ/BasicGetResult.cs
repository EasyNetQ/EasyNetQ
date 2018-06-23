using System;

namespace EasyNetQ
{
    /// <summary>
    /// The result of the AdvancedBus Get method
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IBasicGetResult<T> where T : class
    {
        /// <summary>
        /// True if a message is available, false if not.
        /// </summary>
        bool MessageAvailable { get; }

        /// <summary>
        /// The message retrieved from the queue. 
        /// This property will throw a MessageNotAvailableException if no message
        /// was available. You should check the MessageAvailable property before
        /// attempting to access it.
        /// </summary>
        IMessage<T> Message { get; }
    }

    public class BasicGetResult<T> : IBasicGetResult<T> where T : class
    {
        private readonly IMessage<T> message;
        public bool MessageAvailable { get; }

        public IMessage<T> Message
        {
            get
            {
                if (!MessageAvailable)
                {
                    throw new MessageNotAvailableException();
                }
                return message;
            }
        }

        public BasicGetResult(IMessage<T> message)
        {
            MessageAvailable = true;
            this.message = message;
        }

        public BasicGetResult()
        {
            MessageAvailable = false;
        }
    }

    public interface IBasicGetResult
    {
        byte[] Body { get; }
        MessageProperties Properties { get; }
        MessageReceivedInfo Info { get; }
    }

    public class BasicGetResult : IBasicGetResult
    {
        public byte[] Body { get; }
        public MessageProperties Properties { get; }
        public MessageReceivedInfo Info { get; }

        public BasicGetResult(byte[] body, MessageProperties properties, MessageReceivedInfo info)
        {
            Body = body;
            Properties = properties;
            Info = info;
        }
    }

    public class MessageNotAvailableException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public MessageNotAvailableException() : base(
            "No message currently on queue. Check the MessageAvailable property on " + 
            "IBasicGetResult before attempting to access the Message property.")
        {
        }

        public MessageNotAvailableException(string message) : base(message)
        {
        }

        public MessageNotAvailableException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}