using System.Collections.Generic;

namespace JetBlack.MessageBus.Common.Collections
{
    public static class EnumerableExtensions
    {
        public static ISet<T> ToSet<T>(this IEnumerable<T> values)
        {
            return new HashSet<T>(values);
        }
    }
}
