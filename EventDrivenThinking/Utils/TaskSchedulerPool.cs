using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventDrivenThinking.Utils
{
    public class TaskSchedulerPool
    {
        private readonly List<Lazy<TaskScheduler>> _taskSchedulers;

        public TaskSchedulerPool(int maxSize)
        {
            _taskSchedulers = Enumerable.Range(1, maxSize)
                .Select(
                    _ => new Lazy<TaskScheduler>(() => new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler))
                .ToList();
        }

        public TaskScheduler GetTaskScheduler(object o)
        {
            var partition = Math.Abs(o.GetHashCode()) % _taskSchedulers.Count;
            return _taskSchedulers[partition].Value;
        }

    }
    class DisposableAction : IDisposable
    {
        private readonly Action _disposableAction;

        public DisposableAction(Action disposableAction)
        {
            this._disposableAction = disposableAction;
        }

        public void Dispose()
        {
            _disposableAction();
        }
    }


    static class ReaderWriterLockSlimExtensions
    {
        public static IDisposable GetReaderLock(this ReaderWriterLockSlim _lock)
        {
            _lock.EnterReadLock();
            return new DisposableAction(_lock.ExitReadLock);
        }
        public static IDisposable GetWriterLock(this ReaderWriterLockSlim _lock)
        {
            _lock.EnterWriteLock();
            return new DisposableAction(_lock.ExitWriteLock);
        }
    }
    //public class SynchronizedCollection<T> : IList<T>
    //{
    //    class Interator : IEnumerator<T>
    //    {
    //        private readonly ReaderWriterLockSlim _lock;
    //        private readonly IEnumerator<T> _inner;

    //        public Interator(ReaderWriterLockSlim @lock, List<T> inner)
    //        {
    //            _lock = @lock;
    //            _inner = inner.GetEnumerator();
    //            _lock.EnterReadLock();
    //        }

    //        public bool MoveNext()
    //        {
    //            return _inner.MoveNext();
    //        }

    //        public void Reset()
    //        {
    //            _inner.Reset();
    //        }

    //        public T Current { get => _inner.Current; }

    //        object? IEnumerator.Current => Current;

    //        public void Dispose()
    //        {
    //            _lock.ExitReadLock();
    //        }
    //    }
    //    private ReaderWriterLockSlim _lock;
    //    private List<T> _list;
    //    public SynchronizedCollection()
    //    {
    //        _lock = new ReaderWriterLockSlim();
    //        _list = new List<T>();
    //    }

    //    public IEnumerator<T> GetEnumerator()
    //    {
    //        return new Interator(_lock, _list);
    //    }

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return GetEnumerator();
    //    }

    //    public void Add(T item)
    //    {
    //        using (_lock.GetWriterLock())
    //            _list.Add(item);
    //    }

    //    public void Clear()
    //    {
    //        using (_lock.GetWriterLock())
    //            _list.Clear();
    //    }

    //    public bool Contains(T item)
    //    {
    //        using (_lock.GetReaderLock())
    //            return _list.Contains(item);
    //    }

    //    public void CopyTo(T[] array, int arrayIndex)
    //    {
    //        using (_lock.GetReaderLock())
    //            _list.CopyTo(array, arrayIndex);
    //    }

    //    public bool Remove(T item)
    //    {
    //        using (_lock.GetWriterLock())
    //            return _list.Remove(item);
    //    }

    //    public int Count => _list.Count;
    //    public bool IsReadOnly => false;
    //    public int IndexOf(T item)
    //    {
    //        using (_lock.GetReaderLock())
    //            return _list.IndexOf(item);
    //    }

    //    public void Insert(int index, T item)
    //    {
    //        using (_lock.GetWriterLock())
    //            _list.Insert(index, item);
    //    }

    //    public void RemoveAt(int index)
    //    {
    //        using (_lock.GetWriterLock())
    //            _list.RemoveAt(index);
    //    }

    //    public T this[int index]
    //    {
    //        get
    //        {
    //            using (_lock.GetReaderLock())
    //                return _list[index];
    //        }
    //        set
    //        {
    //            using (_lock.GetWriterLock())
    //                _list[index] = value;
    //        }
    //    }
    //}

    public class SynchronizedCollection<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IList, ICollection
    {
        private List<T> items;
        private object sync;

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.SynchronizedCollection`1" /> class. </summary>
        public SynchronizedCollection()
        {
            this.items = new List<T>();
            this.sync = new object();
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.SynchronizedCollection`1" /> class with the object used to synchronize access to the thread-safe collection.</summary>
        /// <param name="syncRoot">The object used to synchronize access the thread-safe collection.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="syncRoot" /> is <see langword="null" />.</exception>
        public SynchronizedCollection(object syncRoot)
        {
            if (syncRoot == null)
                throw (Exception)new ArgumentNullException(nameof(syncRoot));
            this.items = new List<T>();
            this.sync = syncRoot;
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.SynchronizedCollection`1" /> class from a specified enumerable list of elements and with the object used to synchronize access to the thread-safe collection.</summary>
        /// <param name="syncRoot">The object used to synchronize access to the thread-safe collection.</param>
        /// <param name="list">The <see cref="T:System.Collections.Generic.IEnumerable`1" /> collection of elements used to initialize the thread-safe collection.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="syncRoot" /> or <paramref name="list" /> is <see langword="null" />. </exception>
        public SynchronizedCollection(object syncRoot, IEnumerable<T> list)
        {
            if (syncRoot == null)
                throw (Exception)new ArgumentNullException(nameof(syncRoot));
            if (list == null)
                throw (Exception)new ArgumentNullException(nameof(list));
            this.items = new List<T>(list);
            this.sync = syncRoot;
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.SynchronizedCollection`1" /> class from a specified array of elements and with the object used to synchronize access to the thread-safe collection.</summary>
        /// <param name="syncRoot">The object used to synchronize access the thread-safe collection.</param>
        /// <param name="list">The <see cref="T:System.Array" /> of type <paramref name="T" /> elements used to initialize the thread-safe collection.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="syncRoot" /> or <paramref name="list" /> is <see langword="null" />. </exception>
        public SynchronizedCollection(object syncRoot, params T[] list)
        {
            if (syncRoot == null)
                throw (Exception)new ArgumentNullException(nameof(syncRoot));
            if (list == null)
                throw (Exception)new ArgumentNullException(nameof(list));
            this.items = new List<T>(list.Length);
            for (int index = 0; index < list.Length; ++index)
                this.items.Add(list[index]);
            this.sync = syncRoot;
        }

        /// <summary>Gets the number of elements contained in the thread-safe collection.</summary>
        /// <returns>The number of elements contained in the thread-safe, read-only collection.</returns>
        public int Count
        {
            get
            {
                lock (this.sync)
                    return this.items.Count;
            }
        }

        /// <summary>Gets the list of elements contained in the thread-safe collection.</summary>
        /// <returns>The <see cref="T:System.Collections.Generic.IList`1" /> of elements that are contained in the thread-safe, read-only collection.</returns>
        protected List<T> Items
        {
            get
            {
                return this.items;
            }
        }

        /// <summary>Gets the object used to synchronize access to the thread-safe collection.</summary>
        /// <returns>An object used to synchronize access to the thread-safe collection.</returns>
        public object SyncRoot
        {
            get
            {
                return this.sync;
            }
        }

        /// <summary>Gets an element from the thread-safe collection with a specified index.</summary>
        /// <param name="index">The zero-based index of the element to be retrieved from the collection.</param>
        /// <returns>The object in the collection that has the specified <paramref name="index" />.</returns>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The <paramref name="index" /> specified is less than zero or greater than the number of items in the collection.</exception>
        public T this[int index]
        {
            get
            {
                lock (this.sync)
                    return this.items[index];
            }
            set
            {
                lock (this.sync)
                {
                    if (index < 0 || index >= this.items.Count)
                        throw new ArgumentOutOfRangeException(nameof(index), (object) index, "ValueMustBeInRange");
                    this.SetItem(index, value);
                }
            }
        }

        /// <summary>Adds an item to the thread-safe, read-only collection.</summary>
        /// <param name="item">The element to be added to the collection.</param>
        /// <exception cref="T:System.ArgumentException">The value set is <see langword="null" /> or is not of the correct generic type <paramref name="T" /> for the collection.</exception>
        public void Add(T item)
        {
            lock (this.sync)
                this.InsertItem(this.items.Count, item);
        }

        /// <summary>Removes all items from the collection.</summary>
        public void Clear()
        {
            lock (this.sync)
                this.ClearItems();
        }

        /// <summary>Copies the elements of the collection to a specified array, starting at a particular index.</summary>
        /// <param name="array">The destination <see cref="T:System.Array" /> for the elements of type <paramref name="T " />copied from the collection.</param>
        /// <param name="index">The zero-based index in the array at which copying begins.</param>
        public void CopyTo(T[] array, int index)
        {
            lock (this.sync)
                this.items.CopyTo(array, index);
        }

        /// <summary>Determines whether the collection contains an element with a specific value.</summary>
        /// <param name="item">The object to locate in the collection.</param>
        /// <returns>
        /// <see langword="true" /> if the element value is found in the collection; otherwise<see langword=" false" />.</returns>
        /// <exception cref="T:System.ArgumentException">The value set is <see langword="null" /> or is not of the correct generic type <paramref name="T" /> for the collection.</exception>
        public bool Contains(T item)
        {
            lock (this.sync)
                return this.items.Contains(item);
        }

        /// <summary>Returns an enumerator that iterates through the synchronized collection.</summary>
        /// <returns>An <see cref="T:System.Collections.Generic.IEnumerator`1" /> for objects of the type stored in the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            lock (this.sync)
                return (IEnumerator<T>)this.items.GetEnumerator();
        }

        /// <summary>Returns the index of the first occurrence of a value in the collection.</summary>
        /// <param name="item">Removes all items from the collection.</param>
        /// <returns>The zero-based index of the first occurrence of the value in the collection.</returns>
        /// <exception cref="T:System.ArgumentException">The value set is <see langword="null" /> or is not of the correct generic type <paramref name="T" /> for the collection.</exception>
        public int IndexOf(T item)
        {
            lock (this.sync)
                return this.InternalIndexOf(item);
        }

        /// <summary>Inserts an item into the collection at a specified index.</summary>
        /// <param name="index">The zero-based index of the element to be retrieved from the collection.</param>
        /// <param name="item">The object to be inserted into the collection as an element.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The <paramref name="index" /> specified is less than zero or greater than the number of items in the collection.</exception>
        /// <exception cref="T:System.ArgumentException">The value set is <see langword="null" /> or is not of the correct generic type <paramref name="T" /> for the collection.</exception>
        public void Insert(int index, T item)
        {
            lock (this.sync)
            {
                if (index < 0 || index > this.items.Count)
                    throw (Exception)new ArgumentOutOfRangeException(nameof(index), (object)index, "ValueMustBeInRange");
                this.InsertItem(index, item);
            }
        }

        private int InternalIndexOf(T item)
        {
            int count = this.items.Count;
            for (int index = 0; index < count; ++index)
            {
                if (object.Equals((object)this.items[index], (object)item))
                    return index;
            }
            return -1;
        }

        /// <summary>Removes the first occurrence of a specified item from the collection.</summary>
        /// <param name="item">The object to remove from the collection.</param>
        /// <returns>
        /// <see langword="true" /> if item was successfully removed from the collection; otherwise, <see langword="false" />.</returns>
        public bool Remove(T item)
        {
            lock (this.sync)
            {
                int index = this.InternalIndexOf(item);
                if (index < 0)
                    return false;
                this.RemoveItem(index);
                return true;
            }
        }

        /// <summary>Removes an item at a specified index from the collection.</summary>
        /// <param name="index">The zero-based index of the element to be retrieved from the collection.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The <paramref name="index" /> specified is less than zero or greater than the number of items in the collection.</exception>
        public void RemoveAt(int index)
        {
            lock (this.sync)
            {
                if (index < 0 || index >= this.items.Count)
                    throw (Exception)new ArgumentOutOfRangeException(nameof(index), (object)index, "ValueMustBeInRange");
                this.RemoveItem(index);
            }
        }

        /// <summary>Removes all items from the collection.</summary>
        protected virtual void ClearItems()
        {
            this.items.Clear();
        }

        /// <summary>Inserts an item into the collection at a specified index.</summary>
        /// <param name="index">The zero-based index of the collection where the object is to be inserted.</param>
        /// <param name="item">The object to be inserted into the collection.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The <paramref name="index" /> specified is less than zero or greater than the number of items in the collection.</exception>
        /// <exception cref="T:System.ArgumentException">The value set is <see langword="null" /> or is not of the correct generic type <paramref name="T" /> for the collection.</exception>
        protected virtual void InsertItem(int index, T item)
        {
            this.items.Insert(index, item);
        }

        /// <summary>Removes an item at a specified <paramref name="index" /> from the collection.</summary>
        /// <param name="index">The zero-based index of the element to be retrieved from the collection.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The <paramref name="index" /> specified is less than zero or greater than the number of items in the collection.</exception>
        protected virtual void RemoveItem(int index)
        {
            this.items.RemoveAt(index);
        }

        /// <summary>Replaces the item at a specified index with another item.</summary>
        /// <param name="index">The zero-based index of the object to be replaced.</param>
        /// <param name="item">The object to replace </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">The <paramref name="index" /> specified is less than zero or greater than the number of items in the collection.</exception>
        protected virtual void SetItem(int index, T item)
        {
            this.items[index] = item;
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.items).GetEnumerator();
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return true;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return this.sync;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            lock (this.sync)
                ((ICollection)this.items).CopyTo(array, index);
        }

        object IList.this[int index]
        {
            get
            {
                return (object)this[index];
            }
            set
            {
                SynchronizedCollection<T>.VerifyValueType(value);
                this[index] = (T)value;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        int IList.Add(object value)
        {
            SynchronizedCollection<T>.VerifyValueType(value);
            lock (this.sync)
            {
                this.Add((T)value);
                return this.Count - 1;
            }
        }

        bool IList.Contains(object value)
        {
            SynchronizedCollection<T>.VerifyValueType(value);
            return this.Contains((T)value);
        }

        int IList.IndexOf(object value)
        {
            SynchronizedCollection<T>.VerifyValueType(value);
            return this.IndexOf((T)value);
        }

        void IList.Insert(int index, object value)
        {
            SynchronizedCollection<T>.VerifyValueType(value);
            this.Insert(index, (T)value);
        }

        void IList.Remove(object value)
        {
            SynchronizedCollection<T>.VerifyValueType(value);
            this.Remove((T)value);
        }

        private static void VerifyValueType(object value)
        {
            if (value == null)
            {
                if (typeof(T).IsValueType)
                    throw (Exception)new ArgumentException("SynchronizedCollectionWrongTypeNull");
            }
            else if (!(value is T))
                throw (Exception)new ArgumentException("SynchronizedCollectionWrongType1");
        }
    }
}