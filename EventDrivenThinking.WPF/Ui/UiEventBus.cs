using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Core;
using Prism.Events;

namespace EventDrivenThinking.Ui
{
    public class UiEventBus : IUiEventBus
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ConcurrentQueue<object> _publishedEvents;
        private static readonly ConcurrentDictionary<Type, Action<Guid, ICommand>> cache = new ConcurrentDictionary<Type, Action<Guid, ICommand>>();


        public UiEventBus(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _publishedEvents = new ConcurrentQueue<object>();
        }

        public IEnumerable PublishedEvents => _publishedEvents;

        public void InvokeCommand<T>(Guid id, T cmd) where T:ICommand
        {
            if (typeof(T) == typeof(ICommand))
            {
                cache.GetOrAdd(cmd.GetType(), BuildWhenFunc)(id, cmd);
            }
            else 
                GetEvent<CommandEnvelope<Guid,T>>().Publish(new CommandEnvelope<Guid, T>(id, cmd));
        }

        private Action<Guid, ICommand> BuildWhenFunc(Type commandType)
        {
            var instanceParam = Expression.Constant(this);
            var idParam = Expression.Parameter(typeof(Guid), "id");
            var cmdParam = Expression.Parameter(typeof(ICommand), "cmd");
            var methodInfo = typeof(UiEventBus).GetMethod(nameof(InvokeCommand)).MakeGenericMethod(commandType);

            var callExpression = Expression.Call(instanceParam, methodInfo, idParam, Expression.Convert(cmdParam, commandType));
            var lambda = Expression.Lambda<Action<Guid, ICommand>>(callExpression, idParam, cmdParam);
            return lambda.Compile();
        }
        public IEventPublisher<T> GetEvent<T>()
        {
            var ev = _eventAggregator.GetEvent<PubSubEvent<T>>();
            return new EventPublisher<T>(ev, this);
        }

        private class EventPublisher<T> : IEventPublisher<T> 
        {
            private readonly PubSubEvent<T> _event;
            private readonly UiEventBus _parent;

            public EventPublisher(PubSubEvent<T> @event, UiEventBus parent)
            {
                _event = @event;
                _parent = parent;
            }

            public void Publish(T args)
            {
                _parent._publishedEvents.Enqueue(args);
                _event.Publish(args);
            }
        }
    }
}