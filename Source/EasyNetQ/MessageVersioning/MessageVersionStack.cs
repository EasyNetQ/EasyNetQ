using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EasyNetQ.MessageVersioning
{
    public class MessageVersionStack : IEnumerable<Type>
    {
        private readonly Stack<Type> messageVersions;

        public MessageVersionStack(Type messageType)
        {
            messageVersions = ExtractMessageVersions(messageType);
        }

        public Type Pop()
        {
            return messageVersions.Pop();
        }

        public bool IsEmpty()
        {
            return !messageVersions.Any();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<Type> GetEnumerator()
        {
            return messageVersions.GetEnumerator();
        }

        private static Stack<Type> ExtractMessageVersions(Type type)
        {
            var messageVersions = new Stack<Type>();
            messageVersions.Push(type);
            while (true)
            {
                var messageType = messageVersions.Peek();
                var supersededType = GetSupersededType(messageType);

                if (supersededType == null)
                    break;

                EnsureVersioningValid(messageType, supersededType);
                messageVersions.Push(supersededType);
            }
            messageVersions.TrimExcess();
            return messageVersions;
        }

        private static Type GetSupersededType(Type type)
        {
            if (type.BaseType == null)
                return null;

            var types = FindSupersedes(type);
            var parentTypes = FindSupersedes(type.BaseType);

            return types.Except(parentTypes).FirstOrDefault();
        }

        private static IEnumerable<Type> FindSupersedes(Type type)
        {
            return type
                .GetInterfaces()
                .Where(t => t.GetTypeInfo().IsGenericType && t.GetGenericTypeDefinition() == typeof(ISupersede<>))
                .SelectMany(t => t.GetGenericArguments());
        }

        private static void EnsureVersioningValid(Type messageType, Type supersededType)
        {
            if (!messageType.GetTypeInfo().IsSubclassOf(supersededType))
                throw new EasyNetQException("Message cannot supersede a type it is not a subclass of. {0} is not a subclass of {1}", messageType.Name, supersededType.Name);
        }
    }
}
