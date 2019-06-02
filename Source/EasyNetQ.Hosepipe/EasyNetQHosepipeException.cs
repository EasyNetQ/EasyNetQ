using System;

namespace EasyNetQ.Hosepipe
{
    public class EasyNetQHosepipeException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public EasyNetQHosepipeException() { }
        public EasyNetQHosepipeException(string message) : base(message) { }
        public EasyNetQHosepipeException(string message, Exception inner) : base(message, inner) { }
    }
}
