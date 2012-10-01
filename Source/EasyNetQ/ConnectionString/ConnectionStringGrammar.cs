using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Sprache;

namespace EasyNetQ.ConnectionString
{
    using UpdateConfiguration = Func<ConnectionConfiguration, ConnectionConfiguration>;

    public class ConnectionStringGrammar
    {
        public static Parser<string> Text = Parse.CharExcept(';').Many().Text();
        public static Parser<ushort> Number = Parse.Number.Select(ushort.Parse);

        public static Parser<UpdateConfiguration> Part = new List<Parser<UpdateConfiguration>>
        {
            // add new connection string parts here
            BuildKeyValueParser("host", Text, c => c.Host),
            BuildKeyValueParser("port", Number, c => c.Port),
            BuildKeyValueParser("virtualHost", Text, c => c.VirtualHost),
            BuildKeyValueParser("requestedHeartbeat", Number, c => c.RequestedHeartbeat),
            BuildKeyValueParser("username", Text, c => c.UserName),
            BuildKeyValueParser("password", Text, c => c.Password),
        }.Aggregate((a, b) => a.Or(b));

        public static Parser<IEnumerable<UpdateConfiguration>> ConnectionStringBuilder =
            from first in Part
            from rest in Parse.Char(';').Then(_ => Part).Many()
            select Cons(first, rest);

        public static Parser<UpdateConfiguration> BuildKeyValueParser<T>(
            string keyName,
            Parser<T> valueParser,
            Expression<Func<ConnectionConfiguration, T>> getter)
        {
            return
                from key in Parse.String(keyName).Token()
                from separator in Parse.Char('=')
                from value in valueParser
                select (Func<ConnectionConfiguration, ConnectionConfiguration>)(c =>
                {
                    CreateSetter(getter)(c, value);
                    return c;
                });
        }

        public static Action<ConnectionConfiguration, T> CreateSetter<T>(Expression<Func<ConnectionConfiguration, T>> getter)
        {
            return CreateSetter<ConnectionConfiguration, T>(getter);
        }

        /// <summary>
        /// Stolen from SO:
        /// http://stackoverflow.com/questions/4596453/create-an-actiont-to-set-a-property-when-i-am-provided-with-the-linq-expres
        /// </summary>
        /// <typeparam name="TContaining"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="getter"></param>
        /// <returns></returns>
        public static Action<TContaining, TProperty> CreateSetter<TContaining, TProperty>(Expression<Func<TContaining, TProperty>> getter)
        {
            if (getter == null)
                throw new ArgumentNullException("getter");

            var memberEx = getter.Body as MemberExpression;

            if (memberEx == null)
                throw new ArgumentException("Body is not a member-expression.");

            var property = memberEx.Member as PropertyInfo;

            if (property == null)
                throw new ArgumentException("Member is not a property.");

            if (!property.CanWrite)
                throw new ArgumentException("Property is not writable.");

            return (Action<TContaining, TProperty>)
                Delegate.CreateDelegate(typeof(Action<TContaining, TProperty>),
                    property.GetSetMethod());
        }

        public static IEnumerable<T> Cons<T>(T head, IEnumerable<T> rest)
        {
            yield return head;
            foreach (var item in rest)
                yield return item;
        }
    }
}