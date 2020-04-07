using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Utils;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.EventInference.EventHandlers
{
    public interface IEventHandlerFactory
    {
        TypeCollection SupportedEventTypes { get; }
        IEventHandlerScope Scope();
    }

    public interface IEventHandlerScope : IDisposable
    {
        IEventHandler<IEvent> CreateHandler(Type eventType);
        IEventHandler<TEvent> CreateHandler<TEvent>() where TEvent : IEvent;
    }
    public abstract class EventHandlerFactoryBase : IEventHandlerFactory
    {
        protected readonly IServiceProvider _serviceProvider;
        
        class ScopeHandler : IEventHandlerScope
        {
            private readonly IServiceScope _scope;
            private readonly EventHandlerFactoryBase _parent;
            public ScopeHandler(EventHandlerFactoryBase  parent)
            {
                _parent = parent;
                _scope = _parent._serviceProvider.CreateScope();
            }

            public void Dispose()
            {
                _scope.Dispose();
            }

            public IEventHandler<IEvent> CreateHandler(Type eventType)
            {
                return _parent.CreateHandler(_scope, eventType);
            }

            public IEventHandler<TEvent> CreateHandler<TEvent>() where TEvent : IEvent
            {
                return _parent.CreateHandler<TEvent>(_scope);
            }
        }
        class Handler : IEventHandler<IEvent>
        {
            readonly Func<EventMetadata, IEvent, Task> _proxy;

            public Handler(Func<EventMetadata, IEvent, Task> proxy)
            {
                this._proxy = proxy;
            }

            public Task Execute(EventMetadata m, IEvent ev)
            {
                return _proxy(m, ev);
            }
        }
        private readonly ConcurrentDictionary<Type, Func<IServiceScope,IEventHandler<IEvent>>> _cache;
        private readonly MethodInfo _genericMethod;

        public abstract TypeCollection SupportedEventTypes { get; }
        public IEventHandlerScope Scope()
        {
            return new ScopeHandler(this);
        }

        protected EventHandlerFactoryBase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _cache = new ConcurrentDictionary<Type, Func<IServiceScope,IEventHandler<IEvent>>>();
            _genericMethod = typeof(EventHandlerFactoryBase).GetMethod(nameof(CreateHandlerCore),
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, 
                null, 
                new Type[] {typeof(IServiceScope)}, 
                null);
        }

        private IEventHandler<IEvent> CreateHandler(IServiceScope scope, Type eventType)
        {
            return _cache.GetOrAdd(eventType, (key) =>
            {
                var thisInstance = Expression.Constant(this);
                var mth = _genericMethod.MakeGenericMethod(key);
                var param = Expression.Parameter(typeof(IServiceScope), "scope");
                var call = Expression.Call(thisInstance, mth, param);
                
                var result= Expression.Lambda<Func<IServiceScope,IEventHandler<IEvent>>>(call, param).Compile();
                return result;
            })(scope);
        }

        private IEventHandler<IEvent> CreateHandlerCore<TEvent>(IServiceScope scope) where TEvent : IEvent
        {
            var handler = CreateHandler<TEvent>(scope);
            return new Handler((m,e) => handler.Execute(m,(TEvent)e));
        }
        protected abstract IEventHandler<TEvent> CreateHandler<TEvent>(IServiceScope scope) where TEvent : IEvent;
    }
}