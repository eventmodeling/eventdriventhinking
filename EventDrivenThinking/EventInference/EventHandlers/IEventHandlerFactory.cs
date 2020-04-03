using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Utils;
using Google.Protobuf.Reflection;

namespace EventDrivenThinking.EventInference.EventHandlers
{
    public interface IEventHandlerFactory
    {
        TypeCollection SupportedEventTypes { get; }
        IEventHandler<IEvent> CreateHandler(Type eventType);
        IEventHandler<TEvent> CreateHandler<TEvent>() where TEvent : IEvent;
    }

    public abstract class EventHandlerFactoryBase : IEventHandlerFactory
    {
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
        private readonly ConcurrentDictionary<Type, Func<IEventHandler<IEvent>>> _cache;
        private readonly MethodInfo _genericMethod;

        public abstract TypeCollection SupportedEventTypes { get; }

        protected EventHandlerFactoryBase()
        {
            _cache = new ConcurrentDictionary<Type, Func<IEventHandler<IEvent>>>();
            _genericMethod = typeof(EventHandlerFactoryBase).GetMethod(nameof(CreateHandlerCore),
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod, 
                null, 
                new Type[0], 
                null);
        }

        public IEventHandler<IEvent> CreateHandler(Type eventType)
        {
            return _cache.GetOrAdd(eventType, (key) =>
            {
                var thisInstance = Expression.Constant(this);
                var mth = _genericMethod.MakeGenericMethod(key);
                var call = Expression.Call(thisInstance, mth);

                var result= Expression.Lambda<Func<IEventHandler<IEvent>>>(call).Compile();
                return result;
            })();
        }

        private IEventHandler<IEvent> CreateHandlerCore<TEvent>() where TEvent : IEvent
        {
            var handler = CreateHandler<TEvent>();
            return new Handler((m,e) => handler.Execute(m,(TEvent)e));
        }
        public abstract IEventHandler<TEvent> CreateHandler<TEvent>() where TEvent : IEvent;
    }
}