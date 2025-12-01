namespace EasyNetQ.Tests;

internal sealed class StaticCorrelationIdGenerationStrategy : ICorrelationIdGenerationStrategy
{
    private readonly string correlationId;

    public StaticCorrelationIdGenerationStrategy(string correlationId)
    {
        this.correlationId = correlationId;
    }

    public string GetCorrelationId()
    {
        return correlationId;
    }
}
