namespace EasyNetQ
{
    /// <summary>
    ///     A strategy of generation a correlation identifier
    /// </summary>
    public interface ICorrelationIdGenerationStrategy
    {
        /// <summary>
        ///     Generates a new correlation identifier
        /// </summary>
        /// <returns>New correlation identifier</returns>
        string GetCorrelationId();
    }
}
