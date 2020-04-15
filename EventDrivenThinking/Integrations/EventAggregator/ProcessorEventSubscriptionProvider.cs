using System;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using Prism.Events;

namespace EventDrivenThinking.Integrations.EventAggregator
{
    public class ProcessorEventSubscriptionProvider<TEvent> : 
        IEventSubscriptionProvider<IProcessor, IProcessorSchema, TEvent>
        where TEvent : IEvent
    {
        public string Type => "EventAggregator";
        private IProcessorSchema _schema;
        private readonly IEventAggregator _eventAggregator;

        public ProcessorEventSubscriptionProvider(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        public void Init(IProcessorSchema schema)
        {
            _schema = schema;
        }

        public bool CanMerge(ISubscriptionProvider<IProcessor, IProcessorSchema> other)
        {
            return false;
        }

        public ISubscriptionProvider<IProcessor, IProcessorSchema> Merge(ISubscriptionProvider<IProcessor, IProcessorSchema> other)
        {
            throw new NotImplementedException();
        }

        public async Task<ISubscription> Subscribe(IEventHandlerFactory factory, object[] args = null)
        {
            Subscription s = new Subscription(true);
            if (_schema.Events.Contains(typeof(TEvent)) && factory.SupportedEventTypes.Contains<TEvent>())
            {
                var sub = _eventAggregator.GetEvent<PubSubEvent<EventEnvelope<TEvent>>>();
                
                sub.Subscribe(ev =>
                {
                    using (var scope = factory.Scope())
                    {
                        var handler = scope.CreateHandler<TEvent>();

                        handler.Execute(ev.Metadata, ev.Event).GetAwaiter().GetResult();
                    }
                }, ThreadOption.PublisherThread, true);
            }

            return s;
        }
    }
}