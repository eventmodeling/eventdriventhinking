using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.InMemory;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.EventInference.Subscriptions;
using EventDrivenThinking.Integrations.EventAggregator;
using EventDrivenThinking.Logging;
using EventDrivenThinking.Utils;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EventDrivenThinking.App.Configuration.EventAggregator
{
    /// <summary>
    /// This part is very messy. 
    /// </summary>
    public class ProjectionEventSliceStartup : ISliceStartup<IEventSchema>
    {
        private IEventSchema[] _events;
        private static ILogger Log = LoggerFactory.For<ProjectionEventSliceStartup>();
        public void Initialize(IEnumerable<IEventSchema> events)
        {
            this._events = events.ToArray();
        }

        public void RegisterServices(IServiceCollection serviceCollection)
        {

            foreach (var i in _events)
            {
                Log.Debug("{eventName} is subscribed for projection subscriptions in EventAggregator.", i.Type.Name);
                var service = typeof(IEventSubscriptionProvider<,,>).MakeGenericType(typeof(IProjection), typeof(IProjectionSchema), i.Type);
                var impl = typeof(ProjectionEventSubscriptionProvider<>).MakeGenericType(i.Type);
                serviceCollection.AddSingleton(service, impl);


                service = typeof(IEventSubscriptionProvider<,,>).MakeGenericType(typeof(IProjectionEventStream), typeof(IProjectionSchema), i.Type);
                impl = typeof(ProjectionEventStreamSubscriptionProvider<>).MakeGenericType(i.Type);
                serviceCollection.AddSingleton(service, impl);
            }
        }

        public async Task ConfigureServices(IServiceProvider serviceProvider)
        {
            // Now we can product projection streams easily here.
            // TODO:
            // 1) Produce projection stream here
            // 2) Check how events are subscribed in ProjectionSliceStartup for EventAggregator
            // 3) Check if we should not produce EventStore projection streams here as well. 
            // YES, YES, YES
            // 4) Run, Run, Run...

            IProjectionStreamSubscriptionController controller = new ProjectionStreamSubscriptionController2(serviceProvider);
            
            // workaround
            var projections = _events.SelectMany(x => x.Projections).Distinct().ToArray();

            foreach (var p in projections)
            {
                // so factory needs to be specific for EventAggregator. Because constructed
                // ProjectionStreamEventHandler (by the factory) needs to have right types of ProjectionEventStreams.
                var factoryType = typeof(ProjectionStreamEventHandlerFactory<>).MakeGenericType(p.Type);
                EventHandlerFactoryBase factory =
                    (EventHandlerFactoryBase) ActivatorUtilities.CreateInstance(serviceProvider, factoryType, p);

                // controller resolves stuff with ISubscriptionProvider<TOwner, TSchema> interface.
                // controller in case EventAggregator will produce many subscriber providers
                // in case EventStore it will produce one. 
                await controller.SubscribeHandlers(p, factory);
            }
        }
    }
    class ProjectionStreamEventHandlerFactory<TProjection> : EventHandlerFactoryBase 
    where TProjection:IProjection
    {
        private readonly IInMemoryProjectionStreamRegister _streamRegister;
        private readonly IEnumerable<IProjectionStreamPartitioner<TProjection>> _partitioners;

        public ProjectionStreamEventHandlerFactory(IProjectionSchema schema, 
            IServiceProvider serviceProvider, 
            IEnumerable<IProjectionStreamPartitioner<TProjection>> partitioners, IInMemoryProjectionStreamRegister streamRegister) : base(serviceProvider)
        {
            _partitioners = partitioners;
            _streamRegister = streamRegister;
            SupportedEventTypes = schema.Events;
        }

        public override TypeCollection SupportedEventTypes { get; }
        protected override IEventHandler<TEvent> CreateHandler<TEvent>(IServiceScope scope)
        {
            return new ProjectionStreamEventHandler<TProjection, TEvent>(_streamRegister.CreateOrGet<TProjection>(), _partitioners);
        }
    }
}