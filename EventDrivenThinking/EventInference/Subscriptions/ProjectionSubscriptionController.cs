using System;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public class ProjectionSubscriptionController : SubscriptionController<IProjection, IProjectionSchema>, 
        IProjectionSubscriptionController
    {
        public ProjectionSubscriptionController(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}