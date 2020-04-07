using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.InMemory;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventDrivenThinking.Integrations.EventStore;
using Prism.Events;


namespace EventDrivenThinking.Integrations.EventAggregator
{
    public class ProjectionEventSubscriptionProvider<TEvent> : SingleEventSubscriptionProvider,
        IEventSubscriptionProvider<IProjection, IProjectionSchema, TEvent>
        where TEvent : IEvent
    {
        
        public override Type EventType => typeof(TEvent);
        private Func<IProjectionSchema, IEventHandlerFactory, object[], Task<ISubscription>> _invocation;
        public override async Task<ISubscription> Subscribe( IEventHandlerFactory factory, object[] args = null)
        {
            if (_invocation == null)
            {
                MethodInfo m = typeof(ProjectionEventSubscriptionProvider<TEvent>).GetMethod(nameof(Subscribe),
                    BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic);

                m = m.MakeGenericMethod(_schema.Type);

                var schemaParam = Expression.Parameter(typeof(IProjectionSchema), "schema");
                var factoryParam = Expression.Parameter(typeof(IEventHandlerFactory), "factory");
                var argsParam = Expression.Parameter(typeof(object[]), "args");

                var call = Expression.Call(Expression.Constant(this), m, schemaParam, factoryParam, argsParam);
                _invocation = Expression
                    .Lambda<Func<IProjectionSchema, IEventHandlerFactory, object[], Task<ISubscription>>>(call, schemaParam, factoryParam, argsParam).Compile();
            }

            return await _invocation(_schema, factory, args);
        }

        private async Task<ISubscription> Subscribe<TProjection>(IProjectionSchema schema, IEventHandlerFactory factory, object[] args)
        {
            Subscription s = new Subscription(true);
            if (!factory.SupportedEventTypes.Contains<TEvent>())
                throw new InvalidOperationException($"Event Handler Factory seems not to support this Event. {typeof(TEvent).Name}");


            _eventAggregator.GetEvent<PubSubEvent<ProjectionEvent<TProjection>>>().Subscribe(e =>
            {
                if (e.Event.Event.GetType() == typeof(TEvent))
                {
                    using (var scope = factory.Scope())
                    {
                        var handler = scope.CreateHandler<TEvent>();

                        handler.Execute(e.Event.Metadata, (TEvent) e.Event.Event).GetAwaiter().GetResult();
                    }
                }
            }, ThreadOption.UIThread);
            return s;
        }

        public ProjectionEventSubscriptionProvider(IEventConverter eventConverter, IEventAggregator eventAggregator) : base(eventConverter, eventAggregator)
        {
        }
    }
    public abstract class SingleEventSubscriptionProvider :
        ISubscriptionProvider<IProjection, IProjectionSchema>
    {
        protected readonly IEventAggregator _eventAggregator;
        protected readonly IEventConverter _eventConverter;
        protected IProjectionSchema _schema;

        public void Init(IProjectionSchema schema)
        {
            _schema = schema;
        }
        protected SingleEventSubscriptionProvider( IEventConverter eventConverter, IEventAggregator eventAggregator)
        {
            _eventConverter = eventConverter;
            _eventAggregator = eventAggregator;
        }

        public abstract Type EventType { get; }
        public virtual bool CanMerge(ISubscriptionProvider<IProjection, IProjectionSchema> other)
        {
            return other is SingleEventSubscriptionProvider || other is MultiEventSubscriptionProvider;
        }

        public virtual ISubscriptionProvider<IProjection, IProjectionSchema> Merge(ISubscriptionProvider<IProjection, IProjectionSchema> other)
        {
            if (other is MultiEventSubscriptionProvider multiProvider)
            {
                return other.Merge(this);
            }
            else return new MultiEventSubscriptionProvider(this, _eventConverter, _eventAggregator, _schema);
        }

        public abstract Task<ISubscription> Subscribe(IEventHandlerFactory factory, object[] args = null);

    }
    public class MultiEventSubscriptionProvider : ISubscriptionProvider<IProjection, IProjectionSchema>
    {
        readonly List<SingleEventSubscriptionProvider> _providers;
        private IEventAggregator _eventAggregator;
        
        private readonly IEventConverter _eventConverter;
        protected IProjectionSchema _schema;

        public void Init(IProjectionSchema schema)
        {
            _schema = schema;
        }
        public MultiEventSubscriptionProvider(SingleEventSubscriptionProvider singleEventSubscription, IEventConverter eventConverter, IEventAggregator eventAggregator,
            IProjectionSchema schema)
        {
            _eventConverter = eventConverter;
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
            }, ThreadOption.UIThread);

            return new Subscription(true);
        }
    }
}
