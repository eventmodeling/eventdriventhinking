using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Schema
{
    public class ProcessorSchemaRegister : IProcessorSchemaRegister
    {
        [DebuggerDisplay("Type: {Type.Name} Category: {Category}")]
        class ProcessorSchema : IProcessorSchema
        {
            public Type Type { get; private set; }
            public string Category { get; private set; }
            private readonly List<Type> _events;
            public IEnumerable<Type> Events
            {
                get => _events.AsReadOnly();
            }
            public void AddEventType(Type eventType) { _events.Add(eventType); }
            public ProcessorSchema(Type processorType, string category)
            {
                Type = processorType;
                Category = category;
                _events = new List<Type>();
                _tags = new Lazy<HashSet<string>>(() => new HashSet<string>(Type.FullName.Split('.')));
            }
            private readonly Lazy<HashSet<string>> _tags;
            public IEnumerable<string> Tags => _tags.Value;
            public bool IsTaggedWith(string tag)
            {
                return _tags.Value.Contains(tag);
            }

        }
        private readonly Dictionary<Type, ProcessorSchema> _event2ProcessorType;
        private readonly List<IProcessorSchema> _metadata;
        public ProcessorSchemaRegister()
        {
            _event2ProcessorType = new Dictionary<Type, ProcessorSchema>();
            _metadata = new List<IProcessorSchema>();
        }

        public IProcessorSchema FindByEvent(Type evType)
        {
            return _event2ProcessorType[evType];
        }

        public void Discover(IEnumerable<Type> types)
        {
            var processorTypes = types
                .Where(t => typeof(IProcessor).IsAssignableFrom(t) && !t.IsAbstract)
                .ToArray();

            foreach (var type in processorTypes)
            {
                ProcessorSchema m = new ProcessorSchema(type, ServiceConventions.GetCategoryFromNamespace(type.Namespace));
                _metadata.Add(m);

                foreach (var whenMethod in GetWhenMethods(type)) // We should throw exception on every method that has a name When but unsupported signature.
                {
                    var eventType = whenMethod.GetParameters()[1].ParameterType;
                    _event2ProcessorType.TryAdd(eventType, m);
                    m.AddEventType(eventType);
                }
            }

            Events = _event2ProcessorType.Keys.ToArray();
        }



        public Type[] Events { get; private set; }

        private IEnumerable<MethodInfo> GetWhenMethods(Type t)
        {
            var whens = t.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);


            // BindingFlags.Public
            return whens.Where(x => x.Name == "When"
                                    && x.GetParameters().Length == 2
                                    && !x.IsGenericMethod
                                    && typeof(EventMetadata).IsAssignableFrom(x.GetParameters()[0].ParameterType)
                                    && typeof(IEvent).IsAssignableFrom(x.GetParameters()[1].ParameterType));
        }

        public IEnumerator<IProcessorSchema> GetEnumerator()
        {
            return _metadata.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}