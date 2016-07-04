using System;
using System.Runtime.Serialization;

namespace EasyNetQ.Producer
{
#if !NET_CORE
    [Serializable]
#endif
    public class PublishNackedException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public PublishNackedException()
        {
        }

        public PublishNackedException(string message) : base(message)
        {
        }

        public PublishNackedException(string message, Exception inner) : base(message, inner)
        {
        }

#if !NET_CORE
        protected PublishNackedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
#endif
    }
}