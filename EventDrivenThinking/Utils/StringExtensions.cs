using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace EventDrivenThinking.Utils
{
    public static class CollectionExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key,
            Func<TKey, TValue> onAdd)
        {
            if (dict is ConcurrentDictionary<TKey, TValue> cdict)
                return cdict.GetOrAdd(key, onAdd);

            if(!dict.TryGetValue(key, out TValue value))
                dict.Add(key, value = onAdd(key));
            return value;
        }
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (dict is ConcurrentDictionary<TKey, TValue> cdict)
                return cdict.GetOrAdd(key, value);

            if (!dict.TryGetValue(key, out TValue value2))
                dict.Add(key, value2 = value);
            return value2;
        }

        public static Collection<T> RemoveAll<T>(this Collection<T> collection, IEnumerable<T> items)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            foreach (var each in items)
            {
                collection.Remove(each);
            }

            return collection;
        }
        public static IEnumerable<T> NotNull<T>(this IEnumerable<T> collection)
        {
            if (collection != null)
                return collection;
            return Array.Empty<T>();
        }
        public static Collection<T> RemoveWhen<T>(this Collection<T> collection, Predicate<T> predicate)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var items = collection.Where(x => predicate(x)).ToArray();
            collection.RemoveAll(items);

            return collection;
        }
        public static int IndexOf<T>(this T[] collection, Predicate<T> predicate)
        {
            for (int i = 0; i < collection.Length; i++)
            {
                if (predicate(collection[i]))
                    return i;
            }

            return -1;
        }
    }
    public static class BinarySearchUtils
    {
        public static int BinarySearchIndexOf<TItem>(this IList<TItem> list,
            TItem targetValue, IComparer<TItem> comparer = null)
        {
            Func<TItem, TItem, int> compareFunc =
                comparer != null ? comparer.Compare :
                    (Func<TItem, TItem, int>)Comparer<TItem>.Default.Compare;
            int index = BinarySearchIndexOfBy(list, compareFunc, targetValue);
            return index;
        }

        public static int BinarySearchIndexOfBy<TItem, TValue>(this IList<TItem> list,
            Func<TItem, TValue, int> comparer, TValue value)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            if (comparer == null)
                throw new ArgumentNullException("comparer");

            if (list.Count == 0)
                return -1;

            // Implementation below copied largely from .NET4
            // ArraySortHelper.InternalBinarySearch()
            int lo = 0;
            int hi = list.Count - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = comparer(list[i], value);

                if (order == 0)
                    return i;
                if (order < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return ~lo;
        }
    }
    public static class StringExtensions
    {
        public static T FromJsonBytes<T>(this byte[] data)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data));
        }
        public static byte[] ToJsonBytes<TObject>(TObject obj)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        }
        public static Guid ToGuid(this string str)
        {
            if (!Guid.TryParse(str, out Guid result))
            {
                using (var hash = MD5.Create())
                {
                    hash.Initialize();
                    var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(str));
                    result = new Guid(bytes);
                }
            }

            return result;
        }
    }
    public class ConcurrentHashSet<T> : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly HashSet<T> _hashSet = new HashSet<T>();

        #region Implementation of ICollection<T> ...ish
        public bool Add(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                return _hashSet.Add(item);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _hashSet.Clear();
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            _lock.EnterReadLock();
            try
            {
                return _hashSet.Contains(item);
            }
            finally
            {
                if (_lock.IsReadLockHeld) _lock.ExitReadLock();
            }
        }

        public bool Remove(T item)
        {
            _lock.EnterWriteLock();
            try
            {
                return _hashSet.Remove(item);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _hashSet.Count;
                }
                finally
                {
                    if (_lock.IsReadLockHeld) _lock.ExitReadLock();
                }
            }
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                if (_lock != null)
                    _lock.Dispose();
        }
        ~ConcurrentHashSet()
        {
            Dispose(false);
        }
        #endregion
    }

    
}
