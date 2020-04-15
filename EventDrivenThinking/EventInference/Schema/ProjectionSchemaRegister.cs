using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Models;
using EventDrivenThinking.Utils;

namespace EventDrivenThinking.EventInference.Schema
{
    
    /// <summary>
    /// In a project we can have many projections that update one model. 
    /// However each projection need to have exactly one event that updates the model and that event
    /// is not shared with other projections.
    ///
    /// When a projection subscribes to an event, it check it's source. Based on the source
    /// The infrastructure can understand how to plug appropriate stream.
    ///
    /// For instance a projection might consist of events that some are published locally in event store
    /// Some come from other clients/servers
    ///
    /// The projection produces a stream of events when those events are delivered.
    /// When a projection subscribes for changes it can try to autodetect all sources. For instance:
    /// Sources for projection could be projections stream itself, could be stream per event type, can be global stream. 
    /// </summary>
    public class ProjectionSchemaRegister : IProjectionSchemaRegister
    {
        [DebuggerDisplay("Type: {Type.Name} Category: {Category}")]
        class ProjectionSchema : IProjectionSchema, IEquatable<ProjectionSchema>
        {
            public Type Type { get; private set; }
            public Type ModelType { get; set; }
            public string Category { get; private set; }
            public Guid ProjectionHash { get; }
            public TypeCollection Events { get; }
            public TypeCollection Partitioners { get; }

            public bool Equals(ProjectionSchema other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(Type, other.Type);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ProjectionSchema) obj);
            }

            public override int GetHashCode()
            {
                return (Type != null ? Type.GetHashCode() : 0);
            }

            public static bool operator ==(ProjectionSchema left, ProjectionSchema right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(ProjectionSchema left, ProjectionSchema right)
            {
                return !Equals(left, right);
            }

            
            public void AddEventType(Type eventType) { Events.Add(eventType);}
            
            public ProjectionSchema([NotNull] Type projectionType, string category, params Type[] partitioners)
            {
                Type = projectionType;
                ProjectionHash = projectionType.FullName.ToGuid();
                Category = category;
                Events = new TypeCollection();
                _tags = new Lazy<HashSet<string>>(() => new HashSet<string>(Type.FullName.Split('.')));
                Partitioners = new TypeCollection(partitioners);
            }
            public Type EventByName(string eventEventType)
            {
                return Events.FirstOrDefault(x => x.Name == eventEventType || x.FullName == eventEventType);
            }

            private readonly Lazy<HashSet<string>> _tags;
            public IEnumerable<string> Tags => _tags.Value;
            public bool IsTaggedWith(string tag)
            {
                return _tags.Value.Contains(tag);
            }

            public void MakeReadonly()
            {
                Partitioners.MakeReadonly();
                Events.MakeReadonly();
            }
        }
        private readonly Dictionary<Type, List<ProjectionSchema>> _event2ProjectionType;
        private readonly Dictionary<Type, ProjectionSchema> _modelIndex;
        private readonly List<IProjectionSchema> _metadata;
        public ProjectionSchemaRegister()
        {
            _event2ProjectionType = new Dictionary<Type, List<ProjectionSchema>>(); 
            _metadata = new List<IProjectionSchema>();
            _modelIndex = new Dictionary<Type, ProjectionSchema>();
        }

        public IEnumerable<IProjectionSchema> FindByEvent(Type evType)
        {
            if (_event2ProjectionType.TryGetValue(evType, out var list))
                return list;
            else return Array.Empty<IProjectionSchema>();
        }

        public IProjectionSchema FindByModelType(Type modelType)
        {
            return _modelIndex[modelType];
        }

        public void Discover(IEnumerable<Type> types)
        {
            var projectionTypes = types
                .Where(t => typeof(IProjection).IsAssignableFrom(t) && !t.IsAbstract)
                .ToArray();

            foreach (var type in projectionTypes)
            {
                var projectionPartitionerType = typeof(IProjectionStreamPartitioner<>).MakeGenericType(type);
                var partitionerTypes = types.Where(x => projectionPartitionerType.IsAssignableFrom(x) && !x.IsAbstract)
                    .ToArray();
                ProjectionSchema m = new ProjectionSchema(type, ServiceConventions.GetCategoryFromNamespace(type.Namespace), partitionerTypes);

                var specificProjectionType = type.GetInterface(nameof(IProjection) + "`1");
                if (specificProjectionType != null)
                {
                    m.ModelType = specificProjectionType.GetGenericArguments()[0];
                    _modelIndex.Add(m.ModelType, m);
                }

                _metadata.Add(m);

                foreach (var givenMethods in GetGivenMethods(type))
                {
                    var eventType = givenMethods.GetParameters()[2].ParameterType;

                    var list = _event2ProjectionType.GetOrAdd(eventType, (t) => new List<ProjectionSchema>());
                    list.Add(m);

                    m.AddEventType(eventType);
                }

                m.MakeReadonly();
            }

            Events = _event2ProjectionType.Keys.ToArray();

        }



        public Type[] Events { get; private set; }

        private IEnumerable<MethodInfo> GetGivenMethods(Type t)
        {
            var givens = t.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);


            // BindingFlags.Public
            return givens.Where(x => x.Name == "Given"
                                     && x.GetParameters().Length == 3
                                     && typeof(EventMetadata).IsAssignableFrom(x.GetParameters()[1].ParameterType)
                                     && typeof(IEvent).IsAssignableFrom(x.GetParameters()[2].ParameterType));
        }

        public IEnumerator<IProjectionSchema> GetEnumerator()
        {
            return _metadata.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}