using System;
using System.Collections.Generic;
using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenThinking.EventInference.Schema
{
    class AggregateSchema<T> : IAggregateSchema<T>
        where T : IAggregate
    {
        private readonly IAggregateSchema _schema;
        public IAggregateEventSchema EventByName(string eventEventType)
        {
            return _schema.EventByName(eventEventType);
        }

        public IEnumerable<string> Tags => _schema.Tags;
        public bool IsTaggedWith(string tag)
        {
            return _schema.IsTaggedWith(tag);
        }

        public string Category => _schema.Category;

        public Type Type => _schema.Type;

        public IEnumerable<ICommandSchema> Commands => _schema.Commands;

        public IEnumerable<IAggregateEventSchema> Events => _schema.Events;

        public AggregateSchema(IAggregateSchema schema)
        {
            _schema = schema;
        }
       
    }
}