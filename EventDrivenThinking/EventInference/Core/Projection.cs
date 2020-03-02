using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Core
{
    public static class ProjectionExtensions
    {
        public static async Task Execute(this IProjection projection,
            Func<IEvent, EventMetadata> m,
            params IEvent[] events) 
        {
            await projection.Execute(events.Select(x => (m(x), x)));
        }
    }
    public abstract class Projection<TModel> : IProjection<TModel>
    where TModel:IModel
    {
        private static readonly ConcurrentDictionary<(Type projectionType, Type eventType),
                Func<TModel, EventMetadata, IEvent, Task>>
            ExecuteCache =
                new ConcurrentDictionary<(Type projectionType, Type eventType),
                    Func<TModel, EventMetadata, IEvent, Task>>();

        public Projection(TModel model)
        {
            Model = model;
        }

        public TModel Model { get; }

        IModel IProjection.Model => Model;

        public virtual async Task Execute(IEnumerable<(EventMetadata, IEvent)> events)
        {
            foreach (var (m,ev) in events)
            {
                var eventType = ev.GetType();
                var projectionType = GetType();

                var func = ExecuteCache.GetOrAdd((projectionType, eventType), BuildExecuteCache);
                
                await func(Model, m, ev);
            }
        }

        private Func<TModel, EventMetadata, IEvent, Task> BuildExecuteCache((Type projectionType, Type eventType) key)
        {
            var (projectionType, eventType) = key;

            var modelParam = Expression.Parameter(typeof(TModel), "model");
            var metaParam = Expression.Parameter(typeof(EventMetadata), "schema");
            var eventParam = Expression.Parameter(typeof(IEvent), "event");


            var types = new[] {typeof(TModel), typeof(EventMetadata), eventType};
            var methodInfo = projectionType.GetMethod("Given",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);

            if (methodInfo == null)
                return null;


            var callExpression = Expression.Call(methodInfo, modelParam, metaParam,
                Expression.Convert(eventParam, eventType));
            var lambda =
                Expression.Lambda<Func<TModel, EventMetadata, IEvent, Task>>(callExpression, modelParam, metaParam,
                    eventParam);
            return lambda.Compile();
        }
    }
}