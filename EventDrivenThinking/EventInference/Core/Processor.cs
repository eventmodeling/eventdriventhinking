using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
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
        
        private static readonly ConcurrentDictionary<Type, Func<T,EventMetadata, IEvent, Task<CommandEnvelope<Guid>[]>>> _whenCache = 
            new ConcurrentDictionary<Type, Func<T,EventMetadata, IEvent, Task<CommandEnvelope<Guid>[]>>>();
        public Task<CommandEnvelope<Guid>[]> When<TEvent>(EventMetadata m, TEvent ev) where TEvent : IEvent
        {
            var whenMth = _whenCache.GetOrAdd(ev.GetType(), OnConstructFunc);
            return whenMth((T)this, m, ev);
        }

        private Func<T, EventMetadata, IEvent, Task<CommandEnvelope<Guid>[]>> OnConstructFunc(Type evType)
        {
            var flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
            var types = new []{typeof(EventMetadata), evType};
            var mth = typeof(T).GetMethod("When", flags,null, types,null);

            var evMetadata = Expression.Parameter(typeof(EventMetadata), "m");
            var eventParam = Expression.Parameter(typeof(IEvent), "event");
            var thisExpression = Expression.Parameter(typeof(T), "this");
            var callExpression = mth.IsStatic
                ? Expression.Call(mth, evMetadata, Expression.Convert(eventParam, evType))
                : Expression.Call(thisExpression, mth, evMetadata, Expression.Convert(eventParam, evType));

            var asyncRetOption = typeof(IAsyncEnumerable<(Guid, ICommand)>);
            var retOption = typeof(IEnumerable<(Guid, ICommand)>);

            if (mth.ReturnType == asyncRetOption)
            {
                var lambda =
                    Expression.Lambda<Func<T, EventMetadata, IEvent, IAsyncEnumerable<(Guid, ICommand)>>>(callExpression,
                        thisExpression,
                        evMetadata, eventParam);

                var func = lambda.Compile();
                return async (processor, metadata, ev) =>
                {
                    var result = func(processor, metadata, ev);
                    List<CommandEnvelope<Guid>> commandToExecute  = new List<CommandEnvelope<Guid>>();
                    await foreach (var i in result)
                    {
                        commandToExecute.Add(new CommandEnvelope<Guid>(i.Item1, i.Item2));
                    }

                    return commandToExecute.ToArray();
                };
            }
            else if (mth.ReturnType == retOption)
            {
                var lambda =
                    Expression.Lambda<Func<T, EventMetadata, IEvent, IEnumerable<(Guid, ICommand)>>>(callExpression,
                        thisExpression,
                        evMetadata, eventParam);

                var func = lambda.Compile();

                return (processor, metadata, ev) =>
                {
                    var result = func(processor, metadata, ev)
                        .Select(x => new CommandEnvelope<Guid>(x.Item1, x.Item2))
                        .ToArray();
                    return Task.FromResult(result);
                };
            }
            else throw new InvalidMethodSignatureException(mth);

        }
    }
}