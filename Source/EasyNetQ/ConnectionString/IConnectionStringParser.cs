namespace EasyNetQ.ConnectionString
{
    /// <summary>
    ///     Allows to create ConnectionConfiguration from string
    /// </summary>
    public interface IConnectionStringParser
    {
        /// <summary>
        ///     Parses a connection string to a ConnectionConfiguration
        /// </summary>
        /// <param name="connectionString">The connection string</param>
        /// <returns>Parsed ConnectionConfiguration</returns>
        ConnectionConfiguration Parse(string connectionString);
    }
}
