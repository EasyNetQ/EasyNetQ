using System;

namespace EasyNetQ.AutoSubscribe
{
#if !DOTNET5_4
#endif
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