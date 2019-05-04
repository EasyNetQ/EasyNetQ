using System;
using System.Collections.Generic;

namespace EasyNetQ.Sprache
{
    internal class Input
    {
        private readonly int _column;
        private readonly int _line;
        readonly int _position;
        readonly string _source;

        internal IDictionary<object, object> Memos = new Dictionary<object, object>();

        public Input(string source)
            : this(source, 0)
        {
        }

        internal Input(string source, int position, int line = 1, int column = 1)
        {
            Source = source;

            _source = source;
            _position = position;
            this._line = line;
            this._column = column;
        }

        public string Source { get; set; }

        public char Current => _source[_position];

        public bool AtEnd => _position == _source.Length;

        public int Position => _position;

        public int Line => _line;

        public int Column => _column;

        public Input Advance()
        {
            if (AtEnd)
                throw new InvalidOperationException("The input is already at the end of the source.");

            return new Input(_source, _position + 1, Current == '\n' ? _line + 1 : _line, Current == '\n' ? 1 : _column + 1);
        }

        public override string ToString()
        {
            return string.Format("Line {0}, Column {1}", _line, _column);
        }

        public override bool Equals(object obj)
        {
            var i = obj as Input;
            return i != null && i._source == _source && i._position == _position;
        }

        public override int GetHashCode()
        {
            return _source.GetHashCode() ^ _position.GetHashCode();
        }
    }
}
