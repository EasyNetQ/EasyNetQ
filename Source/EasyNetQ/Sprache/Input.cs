namespace EasyNetQ.Sprache;

internal class Input
{
    private readonly string source;

    public Input(string source) : this(source, 0)
    {
    }

    private Input(string source, int position, int line = 1, int column = 1)
    {
        Source = source;

        this.source = source;
        Position = position;
        Line = line;
        Column = column;
    }

    public string Source { get; }

    public char Current => source[Position];

    public bool AtEnd => Position == source.Length;

    public int Position { get; }

    public int Line { get; }

    public int Column { get; }

    public Input Advance()
    {
        if (AtEnd)
            throw new InvalidOperationException("The input is already at the end of the source.");

        return new Input(source, Position + 1, Current == '\n' ? Line + 1 : Line, Current == '\n' ? 1 : Column + 1);
    }

    public override string ToString()
    {
        return $"Line {Line}, Column {Column}";
    }

    public override bool Equals(object? obj)
    {
        return obj is Input i && i.source == source && i.Position == Position;
    }

    public override int GetHashCode()
    {
        return source.GetHashCode() ^ Position.GetHashCode();
    }
}
