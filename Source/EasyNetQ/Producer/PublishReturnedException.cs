using System;
using System.Runtime.Serialization;

namespace EasyNetQ.Producer
{
    /// <summary>
    ///     This exception indicates that a message was returned
    /// </summary>
    [Serializable]
    public class PublishReturnedException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        /// <inheritdoc />
        public PublishReturnedException()
        {
        }

        /// <inheritdoc />
        public PublishReturnedException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public PublishReturnedException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <inheritdoc />
        protected PublishReturnedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
