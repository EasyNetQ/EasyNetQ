using System.Linq;
using Sprache;

namespace EasyNetQ.ConnectionString
{
    public interface IConnectionStringParser
    {
        ConnectionConfiguration Parse(string connectionString);
    }

    public class ConnectionStringParser : IConnectionStringParser
    {
        public ConnectionConfiguration Parse(string connectionString)
        {
            try
            {
                var updater = ConnectionStringGrammar.ConnectionStringBuilder.Parse(connectionString);
                var connectionConfiguration = updater.Aggregate(new ConnectionConfiguration(), (current, updateFunction) => updateFunction(current));
                connectionConfiguration.Validate();
                return connectionConfiguration;
            }
            catch (ParseException parseException)
            {
                throw new EasyNetQException("Connection String {0}", parseException.Message);
            }
        }
    }
}