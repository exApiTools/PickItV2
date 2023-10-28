using System.Collections.Generic;

namespace PickIt;

public class DefaultDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
{
    private readonly TValue _default;

    public DefaultDictionary(TValue defaultValue)
    {
        _default = defaultValue;
    }

    public DefaultDictionary(IDictionary<TKey, TValue> source, TValue defaultValue) : base(source)
    {
        _default = defaultValue;
    }

    TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key] => TryGetValue(key, out var value) ? value : _default;
}