using System.Linq;
using EasyNetQ.Sprache;

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

    /// <inheritdoc />
    public class ConnectionStringParser : IConnectionStringParser
    {
        /// <inheritdoc />
        public ConnectionConfiguration Parse(string connectionString)
        {
            try
            {
                var updater = ConnectionStringGrammar.ParseConnectionString(connectionString);
                var configuration = updater.Aggregate(new ConnectionConfiguration(),
                    (current, updateFunction) => updateFunction(current));
                configuration.SetDefaultProperties();
                return configuration;
            }
            catch (ParseException parseException)
            {
                throw new EasyNetQException("Connection String {0}", parseException.Message);
            }
        }
    }
}
