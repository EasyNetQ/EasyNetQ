using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using Sprache;

namespace EasyNetQ.ConnectionString
{
    public interface ISslOptionsStringParser
    {
        IEnumerable<SslOption> Parse(string connectionString);
    }

    public class SslOptionsStringParser : ISslOptionsStringParser
    {
        public IEnumerable<SslOption> Parse(string sslOptionsString)
        {
            try
            {
                return SslOptionsGrammar.SslOptionsParser.Parse(sslOptionsString ?? String.Empty);
            }
            catch (ParseException parseException)
            {
                throw new EasyNetQException("SslOptionsString parsing exception: {0}", parseException.Message);
            }

        }
    }
}
