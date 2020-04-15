using System;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.EventInference.Subscriptions
{
    public class ProcessorSubscriptionController : SubscriptionController<IProcessor, IProcessorSchema>,
        IProcessorSubscriptionController
    {
        public ProcessorSubscriptionController(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}