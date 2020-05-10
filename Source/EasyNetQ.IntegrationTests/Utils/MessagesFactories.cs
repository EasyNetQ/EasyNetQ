using System;
using System.Collections.Generic;

namespace EasyNetQ.IntegrationTests.Utils
{
    public static class MessagesFactories
    {
        public static IReadOnlyList<Message> Create(int count)
        {
            return Create(count, i => new Message(i));
        }

        public static IReadOnlyList<Message> Create(int start, int count)
        {
            return Create(start, count, i => new Message(i));
        }

        public static IReadOnlyList<Message> Create(int count, Func<int, Message> factory)
        {
            return Create(0, count, factory);
        }

        public static IReadOnlyList<Message> Create(int start, int count, Func<int, Message> factory)
        {
            var messages = new List<Message>(count);
            for (var i = start; i < start + count; ++i)
                messages.Add(factory(i));
            return messages;
        }
    }
}
