using System.Collections;
using System.Collections.Generic;

namespace SmartX.Shared.Collections;

public class SmartCollection<T> : IEnumerable<T>
{
    private readonly List<T> _items = new();

    public int Count => _items.Count;

    public void Add(T item)
    {
        _items.Add(item);
    }

    public bool Remove(T item)
    {
        return _items.Remove(item);
    }

    public void Clear()
    {
        _items.Clear();
    }

    public T? Find(Predicate<T> match)
    {
        return _items.Find(match);
    }

    public IReadOnlyList<T> GetAll()
    {
        return _items.AsReadOnly();
    }

    public T this[int index] => _items[index];

    public IEnumerator<T> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}