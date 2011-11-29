using System;

namespace EasyNetQ
{
    public class CorrelationIdGenerator
    {
        public static string GetCorrelationId()
        {
            return Guid.NewGuid().ToString();
        } 
    }
}