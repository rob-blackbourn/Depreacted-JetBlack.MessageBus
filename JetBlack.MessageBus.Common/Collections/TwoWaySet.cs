using System.Collections.Generic;

namespace JetBlack.MessageBus.Common.Collections
{
    public class TwoWaySet<TFirst,TSecond>
    {
        private readonly IDictionary<TFirst, ISet<TSecond>> _firstToSeconds = new Dictionary<TFirst, ISet<TSecond>>(); 
        private readonly IDictionary<TSecond, ISet<TFirst>> _secondToFirsts = new Dictionary<TSecond, ISet<TFirst>>();

        public void Add(TFirst first, TSecond second)
        {
            ISet<TSecond> seconds;
            if (!_firstToSeconds.TryGetValue(first, out seconds))
                _firstToSeconds.Add(first, seconds = new HashSet<TSecond>());
            seconds.Add(second);

            ISet<TFirst> firsts;
            if (!_secondToFirsts.TryGetValue(second, out firsts))
                _secondToFirsts.Add(second, firsts = new HashSet<TFirst>());
            firsts.Add(first);
        }

        public void Add(TSecond second, TFirst first)
        {
            Add(first, second);
        }

        public bool ContainsKey(TFirst first)
        {
            return _firstToSeconds.ContainsKey(first);
        }

        public bool ContainsKey(TSecond second)
        {
            return _secondToFirsts.ContainsKey(second);
        }

        public bool TryGetValue(TFirst first, out ISet<TSecond> seconds)
        {
            return _firstToSeconds.TryGetValue(first, out seconds);
        }

        public bool TryGetValue(TSecond second, out ISet<TFirst> firsts)
        {
            return _secondToFirsts.TryGetValue(second, out firsts);
        }

        public IEnumerable<TSecond> Remove(TFirst first)
        {
            ISet<TSecond> seconds;
            if (!_firstToSeconds.TryGetValue(first, out seconds))
                return null;

            var secondsWithoutFirsts = new HashSet<TSecond>();

            foreach (var second in seconds)
            {
                var firsts = _secondToFirsts[second];
                firsts.Remove(first);
                if (firsts.Count == 0)
                {
                    _secondToFirsts.Remove(second);
                    secondsWithoutFirsts.Add(second);
                }
            }

            _firstToSeconds.Remove(first);
            
            return secondsWithoutFirsts;
        }


        public IEnumerable<TFirst> Remove(TSecond second)
        {
            ISet<TFirst> firsts;
            if (!_secondToFirsts.TryGetValue(second, out firsts))
                return null;

            var firstsWithoutSeconds = new HashSet<TFirst>();

            foreach (var first in firsts)
            {
                var seconds = _firstToSeconds[first];
                seconds.Remove(second);
                if (seconds.Count == 0)
                {
                    _firstToSeconds.Remove(first);
                    firstsWithoutSeconds.Add(first);
                }
            }

            _secondToFirsts.Remove(second);

            return firstsWithoutSeconds;
        }

        public ICollection<TFirst> Firsts
        {
            get { return _firstToSeconds.Keys; }
        }

        public ICollection<TSecond> Seconds
        {
            get { return _secondToFirsts.Keys; }
        }
    }
}
