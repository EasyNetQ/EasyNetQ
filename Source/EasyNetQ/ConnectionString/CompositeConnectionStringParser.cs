using System;

namespace EasyNetQ.ConnectionString
{
    internal sealed class CompositeConnectionStringParser : IConnectionStringParser
    {
        private readonly AmqpConnectionStringParser amqpConnectionStringParser;
        private readonly ConnectionStringParser connectionStringParser;

        public CompositeConnectionStringParser(
            AmqpConnectionStringParser amqpConnectionStringParser,
            ConnectionStringParser connectionStringParser
        )
        {
            this.amqpConnectionStringParser = amqpConnectionStringParser;
            this.connectionStringParser = connectionStringParser;
        }

        public ConnectionConfiguration Parse(string connectionString)
        {
            return Uri.TryCreate(connectionString, UriKind.Absolute, out _)
                ? amqpConnectionStringParser.Parse(connectionString)
                : connectionStringParser.Parse(connectionString);
        }
    }
}
