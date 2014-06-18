using System;

namespace EasyNetQ
{
    public interface ICorrelationIdGenerationStrategy
    {
        string GetCorrelationId();
    }
}