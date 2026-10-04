namespace EasyNetQ;

/// <inheritdoc />
public class DefaultCorrelationIdGenerationStrategy : ICorrelationIdGenerationStrategy
{
    /// <inheritdoc />
    public string GetCorrelationId() => Guid.NewGuid().ToString();
}
