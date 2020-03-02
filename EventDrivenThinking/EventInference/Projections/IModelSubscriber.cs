using System;
using System.Collections.Generic;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Projections
{
    /// <summary>
    /// The one that knows how to subscribe for events in ISubscrionManager
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public interface IModelSubscriber<in TModel>
        where TModel : IModel
    {
        void SubscribeModelForQuery<TQuery>(TModel model, TQuery query, Action<IEnumerable<EventEnvelope>> onEvent);
    }
}