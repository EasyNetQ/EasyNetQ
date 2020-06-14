using System;
using System.Runtime.Serialization;

namespace EasyNetQ.Producer
{
    /// <summary>
    ///     This exception indicates that a message was returned
    /// </summary>
    [Serializable]
    public class MessageReturnedException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        /// <summary>
        ///     Creates MessageReturnedException
        /// </summary>
        public MessageReturnedException()
        {
        }

        /// <summary>
        ///     Creates MessageReturnedException
        /// </summary>
        /// <param name="message">The message</param>
        public MessageReturnedException(string message) : base(message)
        {
        }

        public MessageReturnedException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MessageReturnedException(
            SerializationInfo info,
            StreamingContext context
        ) : base(info, context)
        {
        }
    }
}
