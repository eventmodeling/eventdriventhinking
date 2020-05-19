using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenThinking.EventInference.Core
{
    public class AggregateNotInitializedException : Exception
    {
    }
    public interface IId { Guid Id { get; set; } }
    public abstract class Aggregate<TState> : IAggregate
        where TState : new()
    {
        private static readonly ConcurrentDictionary<(Type aggregateType, Type eventType), Func<TState, IEvent, TState>>
            RehydrateFuncCache =
                new ConcurrentDictionary<(Type aggregateType, Type eventType), Func<TState, IEvent, TState>>();

        private static readonly ConcurrentDictionary<(Type aggregateType, Type commandType),
                Func<TState, ICommand, IEvent[]>>
            ExecuteFuncCache =
                new ConcurrentDictionary<(Type aggregateType, Type commandType),
                    Func<TState, ICommand, IEvent[]>>();

        public virtual Guid Id
        {
            get => _id;
            set
            {
                _id = value;
                if (_state is IId _stateId)
                    _stateId.Id = value;
            }
        }

        private ulong _version;
        private Guid _id;
        protected virtual TState _state { get; private set; }
        
        protected Aggregate()
        {
            _state = new TState();
        }

        public ulong Version
        {
            get { return _version; }
            private set => _version = value;
        }


        public void Rehydrate(IEnumerable<IEvent> events)
        {
            foreach (var i in events)
            {
                Apply(i);
                _version += 1;
            }
        }

        public async Task RehydrateAsync(IAsyncEnumerable<IEvent> events)
        {
            await foreach (var i in events)
            {
                Apply(i);
                _version += 1;
            }
        }

        public IEvent[] Execute(ICommand command)
        {
            var cmdType = command.GetType();
            var aggregateType = GetType();
            var func = ExecuteFuncCache.GetOrAdd((aggregateType, cmdType), BuildExecuteFunc);
            return func(_state, command);
        }

        private void Apply(IEvent @event)
        {
            var eventType = @event.GetType();
            var aggregateType = GetType();
            var func = RehydrateFuncCache.GetOrAdd((aggregateType, eventType), BuildRehydrateFunc);
            if (func != null)
                _state = func(_state, @event);
            else Debug.WriteLine($"Warning, no 'Given' method for event: {eventType.Name} in {GetType().Name}.");
        }


        private Func<TState, IEvent, TState> BuildRehydrateFunc((Type aggregateType, Type eventType) key)
        {
            var (aggregateType, eventType) = key;
            var stateParam = Expression.Parameter(typeof(TState), "state");
            var eventParam = Expression.Parameter(typeof(IEvent), "event");

            var methodInfo = aggregateType.GetMethod("Given",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                new[] {typeof(TState), eventType}, null);

            if (methodInfo == null)
                return null;

            var callExpression = Expression.Call(methodInfo, stateParam, Expression.Convert(eventParam, eventType));
            var lambda = Expression.Lambda<Func<TState, IEvent, TState>>(callExpression, stateParam, eventParam);
            return lambda.Compile();
        }

        private Func<TState, ICommand, IEvent[]> BuildExecuteFunc((Type aggregateType, Type commandType) key)
        {
            var (aggregateType, commandType) = key;
            var stateParam = Expression.Parameter(typeof(TState), "state");
            var commandParam = Expression.Parameter(typeof(ICommand), "command");

            var methodInfo = aggregateType.GetMethod("When",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                new[] {typeof(TState), commandType}, null);


            if (methodInfo == null)
                throw new InvalidOperationException(
                    $"No handler for command {commandType.Name} on {aggregateType.Name}.");

            var retParamType = methodInfo.ReturnParameter.ParameterType;

            if (typeof(IEvent).IsAssignableFrom(retParamType) && retParamType.IsClass)
            {
                var oneMth = typeof(OneEnumerable).GetMethod("One").MakeGenericMethod(typeof(IEvent));

                var methodExpression =
                    Expression.Call(methodInfo, stateParam, Expression.Convert(commandParam, commandType));
                var callExpression = Expression.Call(oneMth, Expression.Convert(methodExpression, typeof(IEvent)));
                var lambda =
                    Expression.Lambda<Func<TState, ICommand, IEvent[]>>(callExpression, stateParam,
                        commandParam);
                return lambda.Compile();
            }

            if (retParamType == typeof(IEvent[]))
            {
                var callExpression =
                    Expression.Call(methodInfo, stateParam, Expression.Convert(commandParam, commandType));
                var lambda =
                    Expression.Lambda<Func<TState, ICommand, IEvent[]>>(callExpression, stateParam,
                        commandParam);
                return lambda.Compile();
            }

            if (retParamType == typeof(IEnumerable<IEvent>))
            {
                var toArray = typeof(Enumerable).GetMethod("ToArray").MakeGenericMethod(typeof(IEvent));

                var methodExpression =
                    Expression.Call(methodInfo, stateParam, Expression.Convert(commandParam, commandType));
                var callExpression = Expression.Call(toArray, methodExpression);
                var lambda =
                    Expression.Lambda<Func<TState, ICommand, IEvent[]>>(callExpression, stateParam,
                        commandParam);
                return lambda.Compile();
            }

            throw new NotSupportedException();
        }

        public static class OneEnumerable
        {
            public static T[] One<T>(T one)
            {
                if (one == null) return Array.Empty<T>();
                return new[] {one};
            }
        }
    }
}