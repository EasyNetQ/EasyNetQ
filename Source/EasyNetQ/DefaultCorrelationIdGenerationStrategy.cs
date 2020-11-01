using System;

namespace EasyNetQ
{
    /// <inheritdoc />
    public class DefaultCorrelationIdGenerationStrategy : ICorrelationIdGenerationStrategy
    {
        /// <inheritdoc />
        public string GetCorrelationId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
