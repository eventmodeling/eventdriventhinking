using System;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.InMemory;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventDrivenThinking.Utils;
using Prism.Events;

namespace EventDrivenThinking.Integrations.EventAggregator
{
    /// <summary>
    /// Not implemented yet. Maybe we don't need it.
    /// </summary>
    /// <typeparam name="TProjection"></typeparam>
    public class ProjectionMultiEventSubscriptionProvider<TProjection> :
        ISubscriptionProvider<IProjection, IProjectionSchema>
    {
        public string Type { get; }
        private TypeCollection _events;
        private IEventAggregator _eventAggregator;

        public ProjectionMultiEventSubscriptionProvider(TypeCollection events)
        {
            _events = events;
        }

        public void Init(IProjectionSchema schema)
        {
            throw new NotImplementedException();
        }

        public bool CanMerge(ISubscriptionProvider<IProjection, IProjectionSchema> other)
        {
            throw new NotImplementedException();
        }

        public ISubscriptionProvider<IProjection, IProjectionSchema> Merge(ISubscriptionProvider<IProjection, IProjectionSchema> other)
        {
            throw new NotImplementedException();
        }

        public async Task<ISubscription> Subscribe(IEventHandlerFactory factory, object[] args = null)
        {
            Subscription s = new Subscription(true);
            _eventAggregator.GetEvent<PubSubEvent<ProjectionEvent<TProjection>>>().Subscribe(e =>
            {
                var evType = e.Event.Event.GetType();
                if (_events.Contains(evType))
                {
                    using (var scope = factory.Scope())
                    {
                        var handler = scope.CreateHandler(evType);

                        handler.Execute(e.Event.Metadata, e.Event.Event).GetAwaiter().GetResult();
                    }
                }
            }, ThreadOption.UIThread, true);
            return s;
        }
    }
}