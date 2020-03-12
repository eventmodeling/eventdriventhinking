using System;
using System.Collections.Generic;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.EventInference.Projections
{
    public class ModelSubscriber<TModel> : IModelSubscriber<TModel> where TModel : IModel
    {
        readonly IProjectionSchema _schema;
        private readonly ISubscriptionManager _subscriptionManager;

        public ModelSubscriber(IProjectionSchemaRegister schema, ISubscriptionManager subscriptionManager)
        {
            _schema = schema.FindByModelType(typeof(TModel));
            _subscriptionManager = subscriptionManager;
        }

        public void SubscribeModelForQuery<TQuery>(TModel model, 
            TQuery query, 
            Action<IEnumerable<EventEnvelope>> onEvent)
        {
            // uhh we can subscribe for all or customize subscription process.

            _subscriptionManager.Subscribe(_schema.Events, true, onEvent);
        }
    }
}