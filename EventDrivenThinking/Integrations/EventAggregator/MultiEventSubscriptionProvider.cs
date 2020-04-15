using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.InMemory;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventDrivenThinking.Integrations.EventStore;
using Prism.Events;

namespace EventDrivenThinking.Integrations.EventAggregator
{
    public class MultiEventSubscriptionProvider : ISubscriptionProvider<IProjection, IProjectionSchema>
    {
        readonly List<SingleEventSubscriptionProvider> _providers;
        private IEventAggregator _eventAggregator;
        protected IProjectionSchema _schema;

        public string Type => "EventAggregator";

        public void Init(IProjectionSchema schema)
        {
            _schema = schema;
        }
        public MultiEventSubscriptionProvider(SingleEventSubscriptionProvider singleEventSubscription, IEventAggregator eventAggregator,
            IProjectionSchema schema)
        {
        
            _eventAggregator = eventAggregator;
            _providers = new List<SingleEventSubscriptionProvider>() { singleEventSubscription };
            _schema = schema;
        }

        public virtual bool CanMerge(ISubscriptionProvider<IProjection, IProjectionSchema> other)
        {
            return (other is MultiEventSubscriptionProvider || other is SingleEventSubscriptionProvider);
        }

        public virtual ISubscriptionProvider<IProjection, IProjectionSchema> Merge(ISubscriptionProvider<IProjection, IProjectionSchema> other)
        {
            if (other is MultiEventSubscriptionProvider mp)
            {
                _providers.AddRange(mp._providers);
            }
            else
            {
                _providers.Add(other as SingleEventSubscriptionProvider);
            }
            return this;
        }
        private Func<IProjectionSchema, IEventHandlerFactory, object[], Task<ISubscription>> _invocation;
        public async Task<ISubscription> Subscribe( IEventHandlerFactory factory,
            object[] args = null)
        {
            if (_invocation == null)
            {
                MethodInfo m = typeof(MultiEventSubscriptionProvider).GetMethod(nameof(Subscribe),
                    BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic);
                m = m.MakeGenericMethod(_schema.Type);

                var schemaParam = Expression.Parameter(typeof(IProjectionSchema), "schema");
                var factoryParam = Expression.Parameter(typeof(IEventHandlerFactory), "factory");
                var argsParam = Expression.Parameter(typeof(object[]), "args");

                var call = Expression.Call(Expression.Constant(this), m, schemaParam, factoryParam, argsParam);
                _invocation = Expression
                    .Lambda<Func<IProjectionSchema, IEventHandlerFactory, object[], Task<ISubscription>>>(call, schemaParam,
                        factoryParam, argsParam).Compile();
            }

            return await _invocation(_schema, factory, args);
        }
        private async Task<ISubscription> Subscribe<TProjection>(IProjectionSchema schema, IEventHandlerFactory factory, object[] args = null)
        {
            var supportedTypes = _providers.Select(x => x.EventType).ToHashSet();
            
            _eventAggregator.GetEvent<PubSubEvent<ProjectionEvent<TProjection>>>().Subscribe(e =>
            {
                var eventType = e.Event.Event.GetType();
                if (supportedTypes.Contains(eventType))
                {
                    using (var scope = factory.Scope())
                    {
                        var handler = scope.CreateHandler(eventType);

                        handler.Execute(e.Event.Metadata, e.Event.Event).GetAwaiter().GetResult();
                    }
                }
            }, ThreadOption.UIThread,true);

            return new Subscription(true);
        }
    }
}