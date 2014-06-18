using System;

namespace EasyNetQ.Tests
{
    internal class StaticCorrelationIdGenerationStrategy : ICorrelationIdGenerationStrategy
    {
        private readonly string _correlationId;

        public StaticCorrelationIdGenerationStrategy(string correlationId)
        {
            _correlationId = correlationId;
        }
        public string GetCorrelationId()
        {
            return _correlationId;
        }
    }
}