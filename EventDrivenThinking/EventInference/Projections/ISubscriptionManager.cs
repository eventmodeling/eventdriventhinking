using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Projections
{
    /// <summary>
    /// Common interface for all subscribers.
    /// </summary>
    public interface IModelProjectionSubscriber<TModel>
    {
        Task<ISubscription> SubscribeToStream(
            Func<(EventMetadata, IEvent)[], Task> onEvents,
            Action<ISubscription> onLiveStarted = null,
            Guid? partitionId = null,
            long? location = null);
    }

    public interface ISubscription
    {
        
    }
}