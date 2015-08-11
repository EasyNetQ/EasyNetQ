using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using RabbitMQ.Client;
using Sprache;

namespace EasyNetQ.ConnectionString
{
    using UpdateConfiguration = Func<SslOption, SslOption>;

    public static class SslOptionsGrammar
    {
        public static Parser<string> Text = ConfigurationStringSharedGrammar.Text;
        public static Parser<ushort> Number = ConfigurationStringSharedGrammar.Number;
        public static Parser<bool> Bool = ConfigurationStringSharedGrammar.Bool;

        public static Parser<UpdateConfiguration> Part = new List<Parser<UpdateConfiguration>>
        {
            // add new ssloption string parts here
            BuildKeyValueParser("serverName", Text, s => s.ServerName),
            BuildKeyValueParser("certPath", Text, s => s.CertPath),
            BuildKeyValueParser("certPassphrase", Text, s => s.CertPassphrase),
            BuildKeyValueParser("enabled", Bool, s => s.Enabled)
        }.Aggregate((a, b) => a.Or(b));

        public static Parser<IEnumerable<UpdateConfiguration>> SslOptionStringBuilder = Part.ListDelimitedBy(';');

        public static Parser<SslOption> SslOptionParser = SslOptionStringBuilder.Select(f => f.Aggregate(new SslOption(){Enabled = true}, (current, updateFunction) => updateFunction(current)));

        public static Parser<IEnumerable<SslOption>> SslOptionsParser = SslOptionParser.ListDelimitedBy(',').Or(Parse.WhiteSpace.Many().End().Return(Enumerable.Empty<SslOption>()));

        public static Parser<UpdateConfiguration> BuildKeyValueParser<T>(
            string keyName,
            Parser<T> valueParser,
            Expression<Func<SslOption, T>> getter)
        {
            return ConfigurationStringSharedGrammar.BuildKeyValueParser(keyName, valueParser, getter);
        }

    }

}

