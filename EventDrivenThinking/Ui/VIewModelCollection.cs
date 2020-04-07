using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using CommonServiceLocator;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.Ui
{
    public static class ModelExtensions
    {
        public static IViewModelCollection<TViewModel> AsViewModel<TViewModel, TSourceItem>(this ICollection<TSourceItem> collection) 
            where TViewModel : class, IViewModelFor<TSourceItem>
        {
            return new ViewModelCollection<TViewModel, TSourceItem, object>(collection, null);
        }
        public static IViewModelCollection<TViewModel> AsViewModel<TViewModel, TSourceItem, TParent>(this ICollection<TSourceItem> collection, 
            TParent parent, Func<TSourceItem, TParent, TViewModel> factory = null)
            where TViewModel : class, IViewModelChildOf<TParent>
            where TParent:class
        {
            return new ViewModelCollection<TViewModel, TSourceItem, TParent>(collection, parent, factory);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TViewModel">Can implement IViewModelFor<TSourceItem></TSourceItem></typeparam>
    /// <typeparam name="TSourceItem"></typeparam>
    /// <typeparam name="TParent"></typeparam>
    public class ViewModelCollection<TViewModel, TSourceItem, TParent> : IViewModelCollection<TViewModel>
    {
        private readonly TParent _parent;
        private readonly Dictionary<TSourceItem, TViewModel> _map;
        private readonly ICollection<TSourceItem> _source;
        private readonly IServiceProvider _serviceProvider;
        private readonly Func<TSourceItem, TParent, TViewModel> _factory;
        public ViewModelCollection(object source, object parent, Func<TSourceItem, TParent, TViewModel> factory = null)
        {
            if (typeof(TViewModel).IsInterface) throw new InvalidOperationException("ViewModel cannot be an interface.");
            if (typeof(TSourceItem).IsInterface) throw new InvalidOperationException("SourceItem cannot be an interface.");
            if (factory == null)
            {
                _serviceProvider = ServiceLocator.Current.GetInstance<IServiceProvider>();
                _factory = CreateDefaultFactory();
            }
            else
            {
                if(!typeof(IViewModelFor<TSourceItem>).IsAssignableFrom(typeof(TViewModel)))
                    throw new InvalidEnumArgumentException("Provide factory or implement IViewModelFor in TViewModel.");
                _factory = factory;
            }
            _map = new Dictionary<TSourceItem, TViewModel>();
            _source = (ICollection<TSourceItem>) source;
            _parent = (TParent)parent;
            Bind(source);
        }

        private Func<TSourceItem, TParent, TViewModel> CreateDefaultFactory()
        {
            return (item, parent) =>
            {
                var vm = ActivatorUtilities.CreateInstance<TViewModel>(_serviceProvider);
                var p = vm as IViewModelChildOf<TParent>;
                if (p != null && _parent != null)
                    p.SetParent(_parent);

                if(vm is IViewModelFor<TSourceItem> vmf)
                    vmf.LoadChild(item);

                return vm;
            };
        }

        private void Bind(object source)
        {
            INotifyCollectionChanged collectionChanged = source as INotifyCollectionChanged;
            if(collectionChanged == null) throw new ArgumentException("Source is null or does not implement INotifyCollectionChanged interface.");

            collectionChanged.CollectionChanged += OnCollectionChanged;
        }

        public TViewModel this[TSourceItem item]
        {
            get
            {
                if (item == null) return default(TViewModel);
                if (_map.TryGetValue(item, out TViewModel value))
                    return value;
                else
                {
                    var vm = _factory(item, _parent);
                    _map.Add(item, vm);
                    return vm;
                }
            }
        }
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // heuristic
            var newItem = this[(TSourceItem)e.NewItems?[0]];
            var oldItem = this[(TSourceItem)e.OldItems?[0]];
           
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                _map.Remove((TSourceItem) e.OldItems[0]);
            }
            NotifyCollectionChangedEventArgs n = null;
            if(e.Action == NotifyCollectionChangedAction.Replace)
                n = new NotifyCollectionChangedEventArgs(e.Action, newItem, oldItem);
            else if(e.Action == NotifyCollectionChangedAction.Add)
                n = new NotifyCollectionChangedEventArgs(e.Action, newItem);
            else if(e.Action == NotifyCollectionChangedAction.Remove)
                n = new NotifyCollectionChangedEventArgs(e.Action, oldItem, e.OldStartingIndex);
            var toInvoke = CollectionChanged;
            toInvoke?.Invoke(this, n);
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public IEnumerator<TViewModel> GetEnumerator()
        {
            foreach (var i in _source)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TViewModel item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(TViewModel item)
        {
            return _map.ContainsValue(item);
        }

        public void CopyTo(TViewModel[] array, int arrayIndex)
        {
            throw new NotImplementedException("Copy to is not yet implemented.");
        }

        public bool Remove(TViewModel item)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get => _source.Count;
        }
        public bool IsReadOnly
        {
            get => _source.IsReadOnly;
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public interface IViewModelCollection<T> : INotifyCollectionChanged,
        ICollection<T>, INotifyPropertyChanged, IModel
    {

    }


    public class ViewModelCollection<T> : IViewModelCollection<T>
    {
        private readonly IList<T> _inner;

        public ViewModelCollection(IList<T> inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _inner).GetEnumerator();
        }

        public void Add(T item)
        {
            _inner.Add(item);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
            OnPropertyChanged(nameof(Count));
        }

        public void Clear()
        {
            _inner.Clear();
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
            OnPropertyChanged(nameof(Count));
        }

        public bool Contains(T item)
        {
            return _inner.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _inner.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            var index = _inner.IndexOf(item);
            if (index >= 0)
            {
                _inner.RemoveAt(index);
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
                OnPropertyChanged(nameof(Count));
                return true;
            }

            return false;

        }

        public int Count => _inner.Count;

        public bool IsReadOnly => _inner.IsReadOnly;
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            var ev = CollectionChanged;
            ev?.Invoke(this, args);
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, T item, int index)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
        }
        private void OnCollectionChanged(NotifyCollectionChangedAction action, T item)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}