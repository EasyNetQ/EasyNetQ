namespace EasyNetQ
{
    public interface ICorrelationIdGenerationStrategy
    {
        string GetCorrelationId();
    }
}