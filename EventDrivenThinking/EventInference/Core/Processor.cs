using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Core
{
    /// <summary>
    /// Processor have When method that can be static or instance. The signature is:
    /// return IEnumerable<(Guid, ICommand)> When(EventMetadata m, TEvent ev);
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Processor<T> : IProcessor
        where T:Processor<T>
    {
        private static readonly ConcurrentDictionary<Type, Func<T,EventMetadata, IEvent, IEnumerable<(Guid, ICommand)>>> _whenCache = 
            new ConcurrentDictionary<Type, Func<T,EventMetadata, IEvent, IEnumerable<(Guid, ICommand)>>>();
        public IEnumerable<(Guid, ICommand)> When<TEvent>(EventMetadata m, TEvent ev) where TEvent : IEvent
        {
            var whenMth = _whenCache.GetOrAdd(ev.GetType(), OnConstructFunc);
            return whenMth((T)this, m, ev);
        }

        private Func<T, EventMetadata, IEvent, IEnumerable<(Guid, ICommand)>> OnConstructFunc(Type evType)
        {
            var flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance;
            var types = new []{typeof(EventMetadata), evType};
            var mth = typeof(T).GetMethod("When", flags,null, types,null);

            var evMetadata = Expression.Parameter(typeof(EventMetadata), "m");
            var eventParam = Expression.Parameter(typeof(IEvent), "event");
            var thisExpression = Expression.Parameter(typeof(T), "this");
            var callExpression = mth.IsStatic
                ? Expression.Call(mth, evMetadata, Expression.Convert(eventParam, evType))
                : Expression.Call(thisExpression, mth, evMetadata, Expression.Convert(eventParam, evType));

            var lambda =
                Expression.Lambda<Func<T, EventMetadata, IEvent, IEnumerable<(Guid, ICommand)>>>(callExpression, thisExpression,
                    evMetadata, eventParam);
            return lambda.Compile();
        }
    }
}