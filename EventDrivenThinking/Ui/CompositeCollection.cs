using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using EventDrivenThinking.Utils;

namespace EventDrivenThinking.Ui
{
    public class CompositeCollection<T> : IViewModelCollection<T>, 
        ICollection<IViewModelCollection<T>>
    {
        private bool _isOrdered;
        private readonly IComparer<T> _comparer;

        private readonly List<IViewModelCollection<T>> _sources;
        private readonly ObservableCollection<T> _items;
        public CompositeCollection(bool ordered = false, IComparer<T> comparer = null)
        {
            _isOrdered = ordered;
            _comparer = comparer;
            _items = new ObservableCollection<T>();
            _sources = new List<IViewModelCollection<T>>();
            _items.CollectionChanged += (s, e) => RaiseOnCollectionChanged(e);
        }

        private void RaiseOnCollectionChanged(NotifyCollectionChangedEventArgs sourceArgs)
        {
            var cch = CollectionChanged;
            if(cch != null)
            {
                switch (sourceArgs.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        cch(this, new NotifyCollectionChangedEventArgs(sourceArgs.Action, sourceArgs.NewItems, sourceArgs.NewStartingIndex));
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        cch(this, new NotifyCollectionChangedEventArgs(sourceArgs.Action, sourceArgs.OldItems, sourceArgs.OldStartingIndex));
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        cch(this, new NotifyCollectionChangedEventArgs(sourceArgs.Action,sourceArgs.NewItems[0], sourceArgs.OldItems[0], sourceArgs.OldStartingIndex));
                        break;
                    case NotifyCollectionChangedAction.Move:
                        cch(this, new NotifyCollectionChangedEventArgs(sourceArgs.Action, sourceArgs.NewItems[0], sourceArgs.NewStartingIndex, sourceArgs.OldStartingIndex));
                        break;
                    case NotifyCollectionChangedAction.Reset:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        IEnumerator<IViewModelCollection<T>> IEnumerable<IViewModelCollection<T>>.GetEnumerator()
        {
            return _sources.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            _items.Add(item);
        }

        public void Add(IViewModelCollection<T> item)
        {
            _sources.Add(item);
            if (_isOrdered)
            {
               
            }
            else
                _items.AddRange(item);
            Wire(item);
        }

        private void Wire(IViewModelCollection<T> item)
        {
            if(_isOrdered)
                item.CollectionChanged += OnCollectionSortedChanged;
            else
                item.CollectionChanged += OnCollectionChanged;
            item.PropertyChanged += OnPropertyChanged;
        }

        private void InternalAddSortedRange(IEnumerable<T> items)
        {
            foreach (var i in items)
            {
                if (_items.Count == 0)
                {
                    _items.Add(i);
                    continue;
                }

                var index = _items.BinarySearchIndexOf(i, _comparer);
                if (index < 0)
                    index = -index - 1;

                if (index == _items.Count) // last element
                {
                    _items.Add(i);
                } 
                else _items.Insert(index, i); // somewhere in the middle
            }
        }
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var pc = PropertyChanged;
            pc?.Invoke(this, new PropertyChangedEventArgs(e.PropertyName));
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    _items.AddRange(e.NewItems.Cast<T>());
                    break;

                case NotifyCollectionChangedAction.Remove:
                    _items.RemoveAll(e.OldItems.Cast<T>());
                    break;

                case NotifyCollectionChangedAction.Replace:
                    var index = _items.IndexOf((T) e.OldItems[0]);
                    _items[index] = (T) e.NewItems[0];
                    if(e.NewItems.Count > 1)
                        throw new NotSupportedException();
                    break;
                case NotifyCollectionChangedAction.Move:
                    // we dont do anything
                    break;
                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void OnCollectionSortedChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    InternalAddSortedRange(e.NewItems.Cast<T>());
                    break;

                case NotifyCollectionChangedAction.Remove:
                    _items.RemoveAll(e.OldItems.Cast<T>());
                    break;

                case NotifyCollectionChangedAction.Replace:
                    _items.Remove((T) e.OldItems[0]);
                    InternalAddSortedRange(e.NewItems.Cast<T>());
                    if (e.NewItems.Count > 1)
                        throw new NotSupportedException();
                    break;
                case NotifyCollectionChangedAction.Move:
                    // we dont do anything
                    break;
                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Unwire(IViewModelCollection<T> item)
        {
            item.CollectionChanged -= OnCollectionChanged;
        }
        public void Clear()
        {
            _items.Clear();
            _sources.Clear();
        }

        public bool Contains(IViewModelCollection<T> item)
        {
            return _sources.Contains(item);
        }

        public void CopyTo(IViewModelCollection<T>[] array, int arrayIndex)
        {
            foreach (var i in _sources)
                array[arrayIndex++] = i;
        }

        public bool Remove(IViewModelCollection<T> item)
        {
            var result = _sources.Remove(item);
            if (result)
                Unwire(item);
            return result;
        }
        
        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        public bool Remove(T item)
        {
            if(_sources.Any(x=>x.Contains(item)))
                throw new NotSupportedException();

            return _items.Remove(item);
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public T this[int index]
        {
            get { return _items[index]; }
        }

        public bool IsReadOnly
        {
            get => ((ICollection<T>)_items).IsReadOnly;
        }

        public int IndexOf(IViewModelCollection<T> item)
        {
            return _sources.IndexOf(item);
        }

        

        
    }
}