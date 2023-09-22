using System.Collections;

#pragma warning disable S2551 // Shared resources should not be used for locking: we purposefully lock on "this", so that clients can sync on mutative multiple methods too.

namespace SmartIOT.Connector.Core.Util;

internal class CopyOnWriteArrayList<T> : IList<T>, IReadOnlyList<T>, IList
{
    private volatile T[] _data;

    public CopyOnWriteArrayList()
    {
        _data = Array.Empty<T>();
    }

    public CopyOnWriteArrayList(IEnumerable<T> collection)
    {
        _data = collection.ToArray();
    }

    public T this[int index]
    {
        get => _data[index];
        set
        {
            lock (this)
            {
                var data = new T[_data.Length];
                Array.Copy(_data, data, _data.Length);

                data[index] = value;

                _data = data;
            }
        }
    }

    object? IList.this[int index]
    {
        get => _data[index];
        set
        {
            ((IList<T>)this)[index] = (T)value!;
        }
    }

    public int Count => _data.Length;

    public bool IsReadOnly => false;

    public bool IsFixedSize => false;

    public bool IsSynchronized => true;

    public object SyncRoot => this;

    void ICollection<T>.Add(T item)
    {
        InternalAdd(item);
    }

    public int Add(T item)
    {
        return InternalAdd(item);
    }

    public int Add(object? value)
    {
        return InternalAdd((T)value!);
    }

    private int InternalAdd(T item)
    {
        lock (this)
        {
            var data = new T[_data.Length + 1];
            Array.Copy(_data, data, _data.Length);

            data[data.Length - 1] = item;

            _data = data;

            return data.Length - 1;
        }
    }

    public void Clear()
    {
        _data = Array.Empty<T>();
    }

    public bool Contains(T item)
    {
        return _data.Contains(item);
    }

    public bool Contains(object? value)
    {
        return _data.Contains((T)value!);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        var data = _data;

        Array.Copy(data, 0, array, arrayIndex, data.Length);
    }

    public void CopyTo(Array array, int index)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        var data = _data;
        Array.Copy(data, array, data.Length);
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var t in _data)
        {
            yield return t;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        foreach (var t in _data)
        {
            yield return t;
        }
    }

    public int IndexOf(T item)
    {
        var data = _data;

        var comparer = EqualityComparer<T>.Default;

        for (int i = 0; i < data.Length; i++)
        {
            var t = data[i];
            if (comparer.Equals(item, t))
                return i;
        }

        return -1;
    }

    public int IndexOf(object? value)
    {
        return IndexOf((T)value!);
    }

    public void Insert(int index, T item)
    {
        lock (this)
        {
            if (index > _data.Length)
                throw new ArgumentOutOfRangeException(nameof(index), $"index > size {_data.Length}");
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "index < 0");

            var data = new T[_data.Length + 1];

            if (index > 0)
                Array.Copy(_data, 0, data, 0, index);

            data[index] = item;

            if (index < _data.Length)
                Array.Copy(_data, index, data, index + 1, _data.Length - index);

            _data = data;
        }
    }

    public void Insert(int index, object? value)
    {
        Insert(index, (T)value!);
    }

    public bool Remove(T item)
    {
        bool found = false;

        if (_data.Length <= 0)
            return false;

        lock (this)
        {
            var data = new T[_data.Length - 1];

            var comparer = EqualityComparer<T>.Default;

            int j = 0;
            for (int i = 0; i < _data.Length; i++)
            {
                T? t = _data[i];

                if (found || !comparer.Equals(t, item))
                {
                    data[j] = t;
                    j++;
                }
                else
                {
                    found = true;
                }
            }

            if (found)
                _data = data;
        }

        return found;
    }

    public void Remove(object? value)
    {
        Remove((T)value!);
    }

    public void RemoveAt(int index)
    {
        lock (this)
        {
            if (index >= _data.Length)
                throw new ArgumentOutOfRangeException(nameof(index), $"index >= size {_data.Length}");
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "index < 0");

            var data = new T[_data.Length - 1];
            if (index > 0)
                Array.Copy(_data, 0, data, 0, index);

            if (index < _data.Length - 1)
                Array.Copy(_data, index + 1, data, index, _data.Length - index - 1);

            _data = data;
        }
    }

    /// <summary>
    /// This method tries to remove at the specified index, returning false if index bounds are not ok, instead
    /// of throwing <see cref="IndexOutOfRangeException"/>.
    /// </summary>
    public bool TryRemoveAt(int index, out T? item)
    {
        lock (this)
        {
            if (index >= _data.Length || index < 0)
            {
                item = default;
                return false;
            }

            item = _data[index];

            var data = new T[_data.Length - 1];
            if (index > 0)
                Array.Copy(_data, 0, data, 0, index);

            if (index < _data.Length - 1)
                Array.Copy(_data, index + 1, data, index, _data.Length - index - 1);

            _data = data;

            return true;
        }
    }

    public bool Replace(T replaced, T replacing)
    {
        lock (this)
        {
            var index = IndexOf(replaced);
            if (index < 0)
                return false;

            this[index] = replacing;

            return true;
        }
    }

    /// <summary>
    /// This method tries to replace item at specified index, without throwing an <see cref="IndexOutOfRangeException"/>
    /// if the bounds are not ok.
    /// </summary>
    public bool TryReplaceAt(int index, T replacing, out T? replaced)
    {
        lock (this)
        {
            if (index >= _data.Length || index < 0)
            {
                replaced = default;
                return false;
            }

            replaced = this[index];
            this[index] = replacing;

            return true;
        }
    }
}
