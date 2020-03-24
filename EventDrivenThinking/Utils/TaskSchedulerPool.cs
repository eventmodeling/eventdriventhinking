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
    public class SynchronizedCollection<T> : IList<T>
    {
        class Interator : IEnumerator<T>
        {
            private readonly ReaderWriterLockSlim _lock;
            private readonly IEnumerator<T> _inner;

            public Interator(ReaderWriterLockSlim @lock, List<T> inner)
            {
                _lock = @lock;
                _inner = inner.GetEnumerator();
                _lock.EnterReadLock();
            }

            public bool MoveNext()
            {
                return _inner.MoveNext();
            }

            public void Reset()
            {
                _inner.Reset();
            }

            public T Current { get => _inner.Current; }

            object? IEnumerator.Current => Current;

            public void Dispose()
            {
                _lock.ExitReadLock();
            }
        }
        private ReaderWriterLockSlim _lock;
        private List<T> _list;
        public SynchronizedCollection()
        {
            _lock = new ReaderWriterLockSlim();
            _list = new List<T>();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Interator(_lock, _list);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            using (_lock.GetWriterLock())
                _list.Add(item);
        }

        public void Clear()
        {
            using (_lock.GetWriterLock())
                _list.Clear();
        }

        public bool Contains(T item)
        {
            using (_lock.GetReaderLock())
                return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            using (_lock.GetReaderLock())
                _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            using (_lock.GetWriterLock())
                return _list.Remove(item);
        }

        public int Count { get; }
        public bool IsReadOnly { get; }
        public int IndexOf(T item)
        {
            using (_lock.GetReaderLock())
                return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            using (_lock.GetWriterLock())
                _list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            using (_lock.GetWriterLock())
                _list.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                using (_lock.GetReaderLock())
                    return _list[index];
            }
            set
            {
                using (_lock.GetWriterLock())
                    _list[index] = value;
            }
        }
    }
}