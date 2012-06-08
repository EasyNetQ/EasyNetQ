using System;
using System.Runtime.Serialization;

namespace EasyNetQ.InMemoryClient
{
    [Serializable]
    public class InMemoryClientException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public InMemoryClientException()
        {
        }

        public InMemoryClientException(string message) : base(message)
        {
        }

        public InMemoryClientException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InMemoryClientException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}