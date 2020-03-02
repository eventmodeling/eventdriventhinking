using System;
using System.Collections.Generic;

namespace EventDrivenThinking.EventInference.Schema
{
    class ProcessorSchema<T> : IProcessorSchema<T>
    {
        private readonly IProcessorSchema _schema;
        
        public string Category => _schema.Category;

        public IEnumerable<string> Tags => _schema.Tags;

        public bool IsTaggedWith(string tag)
        {
            return _schema.IsTaggedWith(tag);
        }

        public IEnumerable<Type> Events => _schema.Events;

        public Type Type => _schema.Type;

        public ProcessorSchema(IProcessorSchema schema)
        {
            if(schema.Type != typeof(T)) throw new InvalidOperationException();
            _schema = schema;
        }
    }
}