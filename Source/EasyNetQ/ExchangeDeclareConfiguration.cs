using System.Collections.Generic;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public interface IExchangeDeclareConfiguration
    {
        IExchangeDeclareConfiguration AsDurable(bool durable = true);

        IExchangeDeclareConfiguration AsAutoDelete(bool autoDelete = true);

        IExchangeDeclareConfiguration WithAlternateExchange(IExchange alternateExchange);

        IExchangeDeclareConfiguration WithType(string exchangeType = ExchangeType.Fanout);

        IExchangeDeclareConfiguration WithArgument(string name, object value);
    }

    public sealed class ExchangeDeclareConfiguration : IExchangeDeclareConfiguration
    {
        public bool Durable { get; private set; } = true;

        public bool AutoDelete { get; private set; }

        public string Type { get; private set; }

        public IDictionary<string, object> Arguments { get; } = new Dictionary<string, object>();

        public IExchangeDeclareConfiguration AsDurable(bool durable = true)
        {
            Durable = durable;
            return this;
        }

        public IExchangeDeclareConfiguration AsAutoDelete(bool autoDelete = true)
        {
            AutoDelete = autoDelete;
            return this;
        }

        public IExchangeDeclareConfiguration WithAlternateExchange(IExchange alternateExchange)
        {
            return WithArgument("alternate-exchange", alternateExchange.Name);
        }

        public IExchangeDeclareConfiguration WithType(string type = ExchangeType.Fanout)
        {
            Type = type;
            return this;
        }

        public IExchangeDeclareConfiguration WithArgument(string name, object value)
        {
            Arguments[name] = value;
            return this;
        }
    }
}
