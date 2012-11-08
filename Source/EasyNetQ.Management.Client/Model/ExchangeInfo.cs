using System;
using System.Collections.Generic;

namespace EasyNetQ.Management.Client.Model
{
    public class ExchangeInfo
    {
        public string type { get; private set; }
        public bool auto_delete { get; private set; }
        public bool durable { get; private set; }
        public bool @internal { get; private set; }
        public Arguments arguments { get; private set; }

        private readonly string name;

        private readonly ISet<string> exchangeTypes = new HashSet<string>
        {
            "direct", "topic", "fanout", "headers"
        }; 

        public ExchangeInfo(string name, string type) : this(name, type, false, true, false, new Arguments())
        {
        }

        public ExchangeInfo(string name, string type, bool autoDelete, bool durable, bool @internal, Arguments arguments)
        {
            if(string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }   
            if(type == null)
            {
                throw new ArgumentNullException("type");
            }
            if (!exchangeTypes.Contains(type))
            {
                throw new EasyNetQManagementException("Unknown exchange type '{0}', expected one of {1}", 
                    type,
                    string.Join(", ", exchangeTypes));
            }

            this.name = name;
            this.type = type;
            auto_delete = autoDelete;
            this.durable = durable;
            this.@internal = @internal;
            this.arguments = arguments;
        }

        public string GetName()
        {
            return name;
        }
    }
}