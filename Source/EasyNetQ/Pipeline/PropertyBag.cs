namespace EasyNetQ.Pipeline;

/// <summary>
///     A small, allocation-conscious store of typed properties. Entries are kept in a flat array and found by a linear
///     scan over key ids, which beats hashing for the handful of extension values a context typically carries.
///     Value-type values are boxed; reference types are stored as-is.
/// </summary>
/// <remarks>
///     This is a mutable struct meant to be embedded as a field; do not copy it.
/// </remarks>
public struct PropertyBag
{
    private const int InitialCapacity = 4;

    private Entry[]? entries;
    private int count;

    /// <summary>
    ///     Number of values stored
    /// </summary>
    public readonly int Count => count;

    /// <summary>
    ///     Gets the value stored for <paramref name="key" />
    /// </summary>
    public readonly bool TryGet<T>(PropertyKey<T> key, out T value)
    {
        var items = entries;
        if (items is not null)
        {
            var id = key.Id;
            for (var i = 0; i < count; i++)
            {
                if (items[i].Key == id)
                {
                    value = (T)items[i].Value!;
                    return true;
                }
            }
        }

        value = default!;
        return false;
    }

    /// <summary>
    ///     Stores <paramref name="value" /> for <paramref name="key" />, replacing any previous value
    /// </summary>
    public void Set<T>(PropertyKey<T> key, T value)
    {
        var id = key.Id;
        var items = entries;
        if (items is not null)
        {
            for (var i = 0; i < count; i++)
            {
                if (items[i].Key == id)
                {
                    items[i].Value = value;
                    return;
                }
            }

            if (count == items.Length)
            {
                Array.Resize(ref items, items.Length * 2);
                entries = items;
            }
        }
        else
        {
            entries = items = new Entry[InitialCapacity];
        }

        items[count++] = new Entry(id, value);
    }

    /// <summary>
    ///     Removes the value stored for <paramref name="key" />
    /// </summary>
    public bool Remove<T>(PropertyKey<T> key)
    {
        var items = entries;
        if (items is null) return false;

        var id = key.Id;
        for (var i = 0; i < count; i++)
        {
            if (items[i].Key != id) continue;

            count--;
            items[i] = items[count];
            items[count] = default;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Removes all values but keeps the backing storage for reuse
    /// </summary>
    public void Clear()
    {
        if (entries is not null && count > 0)
            Array.Clear(entries, 0, count);
        count = 0;
    }

    private struct Entry
    {
        public Entry(int key, object? value)
        {
            Key = key;
            Value = value;
        }

        public int Key;
        public object? Value;
    }
}
