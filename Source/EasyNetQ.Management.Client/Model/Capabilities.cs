using System;
using System.Collections.Generic;
using EasyNetQ.Management.Client.Dynamic;

namespace EasyNetQ.Management.Client.Model
{
    public class Capabilities : PropertyExpando
    {
        public Capabilities(IDictionary<string, object> properties) : base (properties)
        {            
        }

        public bool BasicNack { get { return GetPropertyOrDefault<Boolean>("BasicNack"); } }
        public bool PublisherConfirms { get { return GetPropertyOrDefault<Boolean>("PublisherConfirms"); } }
        public bool ConsumerCancelNotify { get { return GetPropertyOrDefault<Boolean>("ConsumerCancelNotify"); } }
        public bool ExchangeExchangeBindings { get { return GetPropertyOrDefault<Boolean>("ExchangeExchangeBindings"); } }
    }
}