using System;
using System.Collections.Generic;

namespace EasyNetQ.Sprache
{
    internal class Input
    {
        private readonly string _source;

        internal IDictionary<object, object> Memos = new Dictionary<object, object>();

        public Input(string source)
            : this(source, 0)
        {
        }

        internal Input(string source, int position, int line = 1, int column = 1)
        {
            Source = source;

            _source = source;
            Position = position;
            Line = line;
            Column = column;
        }

        public string Source { get; set; }

        public char Current => _source[Position];

        public bool AtEnd => Position == _source.Length;

        public int Position { get; private set; }

        public int Line { get; private set; }

        public int Column { get; private set; }

        public Input Advance()
        {
            if (AtEnd)
                throw new InvalidOperationException("The input is already at the end of the source.");

            return new Input(_source, Position + 1, Current == '\n' ? Line + 1 : Line, Current == '\n' ? 1 : Column + 1);
        }

        public override string ToString()
        {
            return string.Format("Line {0}, Column {1}", Line, Column);
        }

        public override bool Equals(object obj)
        {
            return obj is Input i && i._source == _source && i.Position == Position;
        }

        public override int GetHashCode()
        {
            return _source.GetHashCode() ^ Position.GetHashCode();
        }
    }
}
