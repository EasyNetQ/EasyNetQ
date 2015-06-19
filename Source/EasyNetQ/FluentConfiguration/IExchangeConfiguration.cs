namespace EasyNetQ.FluentConfiguration
{
    /// <summary>
    /// Allows configuration to be fluently extended without adding overloads to IBus
    /// 
    /// e.g.
    /// x => x.WithDurable(false)
    /// </summary>
    public interface IExchangeConfiguration
    {
        IExchangeConfiguration WithAlternateExchange(string alternateExchange);

        IExchangeConfiguration WithAutoDelete(bool autoDelete);

        IExchangeConfiguration WithDelayed(bool delayed);

        IExchangeConfiguration WithDurable(bool durable);

        IExchangeConfiguration WithInternal(bool @internal);

        IExchangeConfiguration WithPassive(bool passive);
    }

    public class ExchangeConfiguration : IExchangeConfiguration
    {
        public string AlternateExchange { get; set; }

        public bool AutoDelete { get; set; }

        public bool Delayed { get; set; }

        public bool Durable { get; set; }

        public bool Internal { get; set; }

        public bool Passive { get; set; }

        public ExchangeConfiguration()
        {
            Durable = true;
        }

        public IExchangeConfiguration WithAlternateExchange(string alternateExchange)
        {
            AlternateExchange = alternateExchange;
            return this;
        }

        public IExchangeConfiguration WithAutoDelete(bool autoDelete)
        {
            AutoDelete = autoDelete;
            return this;
        }

        public IExchangeConfiguration WithDelayed(bool delayed)
        {
            Delayed = delayed;
            return this;
        }

        public IExchangeConfiguration WithDurable(bool durable)
        {
            Durable = durable;
            return this;
        }

        public IExchangeConfiguration WithInternal(bool @internal)
        {
            Internal = @internal;
            return this;
        }

        public IExchangeConfiguration WithPassive(bool passive)
        {
            Passive = passive;
            return this;
        }
    }
}
