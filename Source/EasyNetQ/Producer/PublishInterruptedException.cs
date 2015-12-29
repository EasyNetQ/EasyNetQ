using System;

namespace EasyNetQ.Producer
{
#if !DOTNET5_4
#endif
    public class PublishInterruptedException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public PublishInterruptedException()
        {
        }

        public PublishInterruptedException(string message)
            : base(message)
        {
        }

        public PublishInterruptedException(string message, Exception inner)
            : base(message, inner)
        {
        }
#if !DOTNET5_4
#endif
    }
}