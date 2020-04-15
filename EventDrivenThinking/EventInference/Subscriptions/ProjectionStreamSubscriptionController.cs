using System;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public class ProjectionStreamSubscriptionController : SubscriptionController<IProjectionEventStream, IProjectionSchema>,
        IProjectionStreamSubscriptionController
    {
        public ProjectionStreamSubscriptionController(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }

    public interface IProjectionStreamSubscriptionController2
    {
        Task<ISubscription> SubscribeHandlers(IEventSchema schema, 
            IEventHandlerFactory factory, 
            Predicate<ISubscriptionProvider<IEvent, IEventSchema>> filter = null,
            params object[] args);
    }

    public class ProjectionStreamSubscriptionController2 : SubscriptionController<IProjectionEventStream, IProjectionSchema>,
        IProjectionStreamSubscriptionController
    {
        public ProjectionStreamSubscriptionController2(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}