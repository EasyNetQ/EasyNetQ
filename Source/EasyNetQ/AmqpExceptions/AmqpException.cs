using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.AmqpExceptions
{
    public class AmqpException
    {
        public AmapExceptionPreface Preface { get; private set; }
        public IList<IAmqpExceptionElement> Elements { get; private set; }

        public AmqpException(AmapExceptionPreface preface, IList<IAmqpExceptionElement> elements)
        {
            Preface = preface;
            Elements = elements;
        }

        private int GetElement<T>() where T : AmqpExceptionIntegerValueElement
        {
            return Elements.OfType<T>().Select(x => x.Value).SingleOrDefault();
        }

        public int Code { get { return GetElement<AmqpExceptionCodeElement>(); } }
        public int ClassId { get { return GetElement<AmqpExceptionClassIdElement>(); } }
        public int MethodId { get { return GetElement<AmqpExceptionMethodIdElement>(); } }

        public static ushort ConnectionClosed = 320;
    }

    public class AmapExceptionPreface
    {
        public string Text { get; private set; }

        public AmapExceptionPreface(string text)
        {
            Text = text;
        }
    }

    public interface IAmqpExceptionElement { }

    public class TextElement : IAmqpExceptionElement
    {
        public string Text { get; private set; }

        public TextElement(string text)
        {
            Text = text;
        }
    }

    public class AmqpExceptionKeyValueElement : IAmqpExceptionElement
    {
        public string Key { get; private set; }
        public string Value { get; private set; }

        public AmqpExceptionKeyValueElement(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    public abstract class AmqpExceptionIntegerValueElement : IAmqpExceptionElement
    {
        public int Value { get; set; }
    }

    public class AmqpExceptionCodeElement : AmqpExceptionIntegerValueElement { }
    public class AmqpExceptionClassIdElement : AmqpExceptionIntegerValueElement { }
    public class AmqpExceptionMethodIdElement : AmqpExceptionIntegerValueElement { }
}