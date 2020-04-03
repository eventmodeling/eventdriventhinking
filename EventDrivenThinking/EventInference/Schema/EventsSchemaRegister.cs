using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EventDrivenThinking.EventInference.Abstractions;

namespace EventDrivenThinking.EventInference.Schema
{
    public sealed class EventsSchemaRegister : IEventSchemaRegister
    {
        private readonly List<EventSchema> _events;
        
        [DebuggerDisplay("Type: {Type.Name} Category: {Category}")]
        class EventSchema : IEventSchema
        {
            public Type Type { get; }
            public string Category { get; }
            

            public EventSchema(Type type, string category)
            {
                Type = type;
                Category = category;
                _tags = new Lazy<HashSet<string>>(() => new HashSet<string>(Type.FullName.Split('.')));
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
            foreach (var t in types.Where(x=> typeof(IEvent).IsAssignableFrom(x) && !x.IsAbstract))
            {
                _events.Add(new EventSchema(t, ServiceConventions.GetCategoryFromNamespace(t.Namespace)));
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