using System;

namespace EasyNetQ.Producer
{
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
    }
}