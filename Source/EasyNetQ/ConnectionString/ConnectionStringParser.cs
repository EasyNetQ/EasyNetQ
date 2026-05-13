using EasyNetQ.Sprache;

namespace EasyNetQ.ConnectionString;

/// <inheritdoc />
public class ConnectionStringParser : IConnectionStringParser
{
    /// <inheritdoc />
    public ConnectionConfiguration Parse(string connectionString)
    {
        try
        {
            var updater = ConnectionStringGrammar.ParseConnectionString(connectionString);
            ConnectionConfiguration config = updater.Aggregate(
                new ConnectionConfiguration(), (current, updateFunction) => updateFunction(current)
            );
            if (config.Ssl.Enabled)
                config.Ssl.ServerName = config.Hosts.First().Host;
            return config;
        }
        catch (ParseException parseException)
        {
            throw new EasyNetQException("Connection String {0}", parseException.Message);
        }
    }
}
