using System;

namespace EasyNetQ.AutoSubscribe
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ForTopicAttribute : Attribute
    {
        public ForTopicAttribute(string topic)
        {
            Topic = topic;
        }

        public string Topic { get; set; }
    }
}