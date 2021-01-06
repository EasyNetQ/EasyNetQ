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
                return updater.Aggregate(
                    new ConnectionConfiguration(), (current, updateFunction) => updateFunction(current)
                );
            }
            catch (ParseException parseException)
            {
                throw new EasyNetQException("Connection String {0}", parseException.Message);
            }
        }
    }
}
