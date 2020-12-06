using System.Linq;
using EasyNetQ.Sprache;

namespace EasyNetQ.ConnectionString
{
    /// <inheritdoc />
    public class ConnectionStringParser : IConnectionStringParser
    {
        /// <inheritdoc />
        public ConnectionConfiguration Parse(string connectionString)
        {
            try
            {
                var updater = ConnectionStringGrammar.ParseConnectionString(connectionString);
                var configuration = updater.Aggregate(
                    new ConnectionConfiguration(), (current, updateFunction) => updateFunction(current)
                );
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
