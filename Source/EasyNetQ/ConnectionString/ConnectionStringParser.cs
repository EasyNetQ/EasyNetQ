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
            return PostProcess(updater.Aggregate(
                new ConnectionConfiguration(), (current, updateFunction) => updateFunction(current)
            ));
        }
        catch (ParseException parseException)
        {
            throw new EasyNetQException("Connection String {0}", parseException.Message);
        }
    }

    private ConnectionConfiguration PostProcess(ConnectionConfiguration configuration)
    {
        foreach (var host in configuration.Hosts)
        {
            host.Ssl.Enabled = configuration.Ssl.Enabled;
        }

        return configuration;
    }
}
