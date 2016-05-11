using System.Collections;
using System.Collections.Generic;

namespace JetBlack.MessageBus.Common.Collections
{
    public class CountedSet<T> : IEnumerable<T>
    {
        private readonly Dictionary<T, int> _cache = new Dictionary<T, int>();

        public CountedSet(IEnumerable<T> values)
        {
            foreach (var value in values)
                _cache.Add(value, 1);
        }

        public int Add(T value)
        {
            if (_cache.ContainsKey(value))
                return ++_cache[value];

            _cache.Add(value, 1);
            return 1;
        }

        public int Remove(T value)
        {
            if (!_cache.ContainsKey(value))
                throw new KeyNotFoundException();

            var count = --_cache[value];
            if (count == 0)
                _cache.Remove(value);
            return count;
        }

        public bool RemoveAll(T value)
        {
            return _cache.Remove(value);
        }

        public bool TryGetCount(T value, out int count)
        {
            return _cache.TryGetValue(value, out count);
        }

        public bool Contains(T value)
        {
            return _cache.ContainsKey(value);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _cache.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_cache.Keys).GetEnumerator();
        }

        public int Count
        {
            get { return _cache.Count; }
        }
    }
}
