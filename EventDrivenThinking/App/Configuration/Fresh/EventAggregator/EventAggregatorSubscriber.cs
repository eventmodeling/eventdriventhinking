using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.Reflection;
using Prism.Events;
using Serilog;

namespace EventDrivenThinking.App.Configuration.Fresh.EventAggregator
{
    class EventAggregatorSubscriber
    {
        private readonly IEventAggregator _aggregator;
        private readonly IEventHandlerDispatcher _dispatcher;
        private readonly ILogger _logger;

        public EventAggregatorSubscriber(IEventAggregator aggregator, IEventHandlerDispatcher dispatcher, ILogger logger)
        {
            this._aggregator = aggregator;
            this._dispatcher = dispatcher;
            this._logger = logger;
        }

        private interface IEventHandlerConfigurator
        {
            Task Configure(IEventAggregator aggregator, IEventHandlerDispatcher dispatcher, ILogger logger);
        }


        private class EventHandlerConfigurator<TEvent> : IEventHandlerConfigurator
            where TEvent : IEvent
        {
            private IEventHandlerDispatcher _dispatcher;

            public Task Configure(IEventAggregator aggregator, IEventHandlerDispatcher dispatcher, ILogger logger)
            {
                this._dispatcher = dispatcher;
                aggregator.GetEvent<PubSubEvent<EventEnvelope<TEvent>>>()
                    .Subscribe(OnEvent, ThreadOption.UIThread, true);
                logger.Information("{eventName} will is subscribed though IEventAggregator and dispatched.", typeof(TEvent).Name);
                return Task.CompletedTask;
            }

            private void OnEvent(EventEnvelope<TEvent> obj)
            {
                Task.Run(() => _dispatcher.Dispatch(obj.Metadata, obj.Event));
            }
        }
        public async Task Subscribe(IEnumerable<Type> eventTypes)
        {
            foreach (var e in eventTypes)
            {
                var configuratorType = typeof(EventHandlerConfigurator<>).MakeGenericType(e);
                var configurator = Ctor<IEventHandlerConfigurator>.Create(configuratorType);
                await configurator.Configure(_aggregator, _dispatcher, _logger);
            }
        }
    }
}