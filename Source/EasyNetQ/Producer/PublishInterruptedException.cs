using System;
using System.Runtime.Serialization;

namespace EasyNetQ.Producer
{
    /// <summary>
    ///     This exception indicates that a publish was interrupted(for instance, because of a reconnection)
    /// </summary>
    [Serializable]
    public class PublishInterruptedException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        /// <inheritdoc />
        public PublishInterruptedException()
        {
        }

        /// <inheritdoc />
        public PublishInterruptedException(string message)
            : base(message)
        {
        }

        /// <inheritdoc />
        public PublishInterruptedException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <inheritdoc />
        protected PublishInterruptedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
