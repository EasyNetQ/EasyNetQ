using System;

namespace EasyNetQ
{
    public class DefaultCorrelationIdGenerationStrategy : ICorrelationIdGenerationStrategy
    {
        public string GetCorrelationId()
        {
            return Guid.NewGuid().ToString();
        } 
    }
}