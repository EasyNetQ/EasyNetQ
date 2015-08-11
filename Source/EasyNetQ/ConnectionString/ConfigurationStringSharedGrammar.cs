using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Sprache;

namespace EasyNetQ.ConnectionString
{
    public static class ConfigurationStringSharedGrammar
    {
        public static Parser<string> Text = Parse.CharExcept(c => c == ';' || c == ',', ";,").Many().Text();
        public static Parser<ushort> Number = Parse.Number.Select(UInt16.Parse);
        
        public static Parser<bool> Bool = (Parse.CaseInsensitiveString("true").Or(Parse.CaseInsensitiveString("false"))).Text().Select(x => x.ToLower() == "true");


        public static Parser<Func<TUpdatable, TUpdatable>> BuildKeyValueParser<T, TUpdatable>(
            string keyName,
            Parser<T> valueParser,
            Expression<Func<TUpdatable, T>> getter)
        {
            return
                from key in Parse.CaseInsensitiveString(keyName).Token()
                from separator in Parse.Char('=')
                from value in valueParser
                select (Func<TUpdatable, TUpdatable>)(c =>
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
            Preconditions.CheckNotNull(getter, "getter");

            var memberEx = getter.Body as MemberExpression;

            Preconditions.CheckNotNull(memberEx, "getter", "Body is not a member-expression.");

            var property = memberEx.Member as PropertyInfo;

            Preconditions.CheckNotNull(property, "getter", "Member is not a property.");
            Preconditions.CheckTrue(property.CanWrite, "getter", "Member is not a writeable property.");

            return (Action<TContaining, TProperty>)
                Delegate.CreateDelegate(typeof(Action<TContaining, TProperty>),
                    property.GetSetMethod());
        }

        public static IEnumerable<T> Cons<T>(this T head, IEnumerable<T> rest)
        {
            yield return head;
            foreach (var item in rest)
                yield return item;
        }

        public static Parser<IEnumerable<T>> ListDelimitedBy<T>(this Parser<T> parser, char delimiter)
        {
            return
                from head in parser
                from tail in Parse.Char(delimiter).Then(_ => parser).Many()
                select head.Cons(tail);
        }
    }
}
