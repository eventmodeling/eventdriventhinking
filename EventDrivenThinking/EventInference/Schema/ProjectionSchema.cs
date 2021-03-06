﻿using System;
using System.Collections.Generic;
using EventDrivenThinking.Utils;

namespace EventDrivenThinking.EventInference.Schema
{
    class ProjectionSchema<T> : IProjectionSchema<T>
    {
        // We need to provide info that will allow to subscribe for Event. 
        // Since events can be stored via many mechanisms.

        private readonly IProjectionSchema _schema;
        public string Category => _schema.Category;

        public Guid ProjectionHash => _schema.ProjectionHash;
        public TypeCollection Events => _schema.Events;
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

        

        public override int GetHashCode()
        {
            return _schema.GetHashCode();
        }

        public TypeCollection Partitioners => _schema.Partitioners;

        public ProjectionSchema(IProjectionSchema schema)
        {
            if (schema.Type != typeof(T)) throw new InvalidOperationException();

            _schema = schema;
        }
    }
}