using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.EventInference.Core
{
    /// <summary>
    ///     Allows more complex calculations though method bindings or builder style bindings.
    /// </summary>
    /// <typeparam name="TProjection"></typeparam>
    public abstract class ProjectionStreamPartitioner<TProjection> : IProjectionStreamPartitioner<TProjection>
        where TProjection : IProjection
    {
        private readonly ConcurrentDictionary<Type, Func<IModel, EventMetadata, IEvent, Guid[]>> _methods =
            new ConcurrentDictionary<Type, Func<IModel, EventMetadata, IEvent, Guid[]>>();

        public Guid[] CalculatePartitions(IModel model, EventMetadata m, IEvent ev)
        {
            var f = _methods.GetOrAdd(ev.GetType(), x => OnBuildCalculateMethod(x, model.GetType()));
            return f?.Invoke(model, m, ev) ?? Array.Empty<Guid>();
        }

        protected IRegistrationSyntax<TModel> FromModel<TModel>()
        {
            return new RegistrationSyntax<TModel>(this);
        }

        private Func<IModel, EventMetadata, IEvent, Guid[]> OnBuildCalculateMethod(Type eventType, Type modelType)
        {
            Type[] args = {modelType, typeof(EventMetadata), eventType};

            // Should go though inheritance hierarchy starting from eventType.
            var method = GetType().GetMethod("CalculatePartition",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, args,
                null);

            if (method == null)
                method = GetType().GetMethod("CalculatePartitions",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                    args, null);

            if (method == null)
                return null;

            if (method.DeclaringType == typeof(ProjectionStreamPartitioner<TProjection>))
                return null;

            var modelParam = Expression.Parameter(typeof(IModel), "model");
            var eventParam = Expression.Parameter(typeof(IEvent), "event");
            var eventMetaParam = Expression.Parameter(typeof(EventMetadata), "metadata");

            var callExpression = Expression.Call(method, Expression.Convert(modelParam, modelType), eventMetaParam,
                Expression.Convert(eventParam, eventType));

            if (method.ReturnType == typeof(Guid))
            {
                var arrayExpression = Expression.NewArrayInit(typeof(Guid), callExpression);
                var lambda = Expression.Lambda<Func<IModel, EventMetadata, IEvent, Guid[]>>(arrayExpression, modelParam,
                    eventMetaParam, eventParam);
                return lambda.Compile();
            }

            if (method.ReturnType == typeof(Guid[]))
            {
                var lambda =
                    Expression.Lambda<Func<IModel, EventMetadata, IEvent, Guid[]>>(callExpression, modelParam,
                        eventMetaParam, eventParam);
                return lambda.Compile();
            }

            throw new InvalidMethodSignatureException(method, "CalculatePartitions should return Guid or Guid[].");
        }

        public interface IRegistrationSyntax<out TModel>
        {
            IRegistrationSyntax<TModel> WithEvent<TEvent>(Func<TModel, EventMetadata, TEvent, Guid> mth);
            IRegistrationSyntax<TModel> WithEvent<TEvent>(Func<TModel, EventMetadata, TEvent, Guid[]> mth);
        }

        private class RegistrationSyntax<TModel> : IRegistrationSyntax<TModel>
        {
            private readonly ProjectionStreamPartitioner<TProjection> _parent;

            public RegistrationSyntax(ProjectionStreamPartitioner<TProjection> parent)
            {
                _parent = parent;
            }

            public IRegistrationSyntax<TModel> WithEvent<TEvent>(Func<TModel, EventMetadata, TEvent, Guid[]> mth)
            {
                _parent._methods.TryAdd(typeof(TEvent),
                    (model, metadata, ev) => mth((TModel) model, metadata, (TEvent) ev));
                return this;
            }

            public IRegistrationSyntax<TModel> WithEvent<TEvent>(Func<TModel, EventMetadata, TEvent, Guid> mth)
            {
                _parent._methods.TryAdd(typeof(TEvent),
                    (model, metadata, ev) => new Guid[1] {mth((TModel) model, metadata, (TEvent) ev)});
                return this;
            }
        }
    }
}