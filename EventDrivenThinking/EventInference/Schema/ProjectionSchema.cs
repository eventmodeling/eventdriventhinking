using System;
using System.Collections.Generic;

namespace EventDrivenThinking.EventInference.Schema
{
    class ProjectionSchema<T> : IProjectionSchema<T>
    {
        private readonly IProjectionSchema _schema;
        public string Category => _schema.Category;

        public IEnumerable<Type> Events => _schema.Events;
        public IEnumerable<string> Tags => _schema.Tags;

        public bool IsTaggedWith(string tag)
        {
            return _schema.IsTaggedWith(tag);
        }

        public Type Type => _schema.Type;

        public Type ModelType => _schema.ModelType;
        public Type EventByName(string eventEventType)
        {
            return _schema.EventByName(eventEventType);
        }

        public IEnumerable<Type> Partitioners => _schema.Partitioners;

        public ProjectionSchema(IProjectionSchema schema)
        {
            if (schema.Type != typeof(T)) throw new InvalidOperationException();

            _schema = schema;
        }
    }
}