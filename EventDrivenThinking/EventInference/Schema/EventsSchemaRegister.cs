using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.Utils;

namespace EventDrivenThinking.EventInference.Schema
{
    public sealed class EventsSchemaRegister : IEventSchemaRegister
    {
        private readonly List<EventSchema> _events;
        
        [DebuggerDisplay("Type: {Type.Name} Category: {Category}")]
        class EventSchema : IEventSchema
        {
            private readonly HashSet<IProjectionSchema> _projections;
            public Type Type { get; }

            public IEnumerable<IProjectionSchema> Projections => _projections;
            public string Category { get; }
            

            public EventSchema(Type type, string category)
            {
                Type = type;
                Category = category;
                _tags = new Lazy<HashSet<string>>(() => new HashSet<string>(Type.FullName.Split('.')));
                _projections = new HashSet<IProjectionSchema>();
            }

            public void AppendProjections(IEnumerable<IProjectionSchema> projectionTypes)
            {
                foreach (var i in projectionTypes) _projections.Add(i);
            }
            private readonly Lazy<HashSet<string>> _tags;
            public IEnumerable<string> Tags => _tags.Value;
            public bool IsTaggedWith(string tag)
            {
                return _tags.Value.Contains(tag);
            }
        }

        public EventsSchemaRegister()
        {
            _events = new List<EventSchema>();
        }
        
        public void Discover(IEnumerable<Type> types)
        {
            ProjectionSchemaRegister helper = new ProjectionSchemaRegister();
            helper.Discover(types);

            foreach (var t in types.Where(x=> typeof(IEvent).IsAssignableFrom(x) && !x.IsAbstract))
            {
                var eventSchema = new EventSchema(t, ServiceConventions.GetCategoryFromNamespace(t.Namespace));
                var findByEvent = helper.FindByEvent(t);
                eventSchema.AppendProjections(findByEvent);
                _events.Add(eventSchema);
            }
        }

        public IEnumerator<IEventSchema> GetEnumerator()
        {
            return _events.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}