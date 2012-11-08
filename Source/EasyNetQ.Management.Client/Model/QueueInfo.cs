using System;

namespace EasyNetQ.Management.Client.Model
{
    public class QueueInfo
    {
        private readonly string name;

        public bool auto_delete { get; private set; }
        public bool durable { get; private set; }
        public InputArguments arguments { get; private set; }

        public QueueInfo(string name, bool autoDelete, bool durable, InputArguments arguments)
        {
            if(string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }
            if(arguments == null)
            {
                throw new ArgumentNullException("arguments");
            }

            this.name = name;
            auto_delete = autoDelete;
            this.durable = durable;
            this.arguments = arguments;
        }

        public QueueInfo(string name) :
            this(name, false, true, new InputArguments())
        {
        }

        public string GetName()
        {
            return name;
        }
    }
}