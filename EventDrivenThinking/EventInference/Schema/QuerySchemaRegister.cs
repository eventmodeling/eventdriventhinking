using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.Reflection;

namespace EventDrivenThinking.EventInference.Schema
{
    public class QuerySchemaRegister : IQuerySchemaRegister
    {
        private readonly List<QuerySchema> _querySchema;
        private readonly Dictionary<Type, QuerySchema> _queryTypeIndex;
        class QuerySchema : IQuerySchema
        {
            public Type Type { get; }
            public string Category { get; }

            public Type ModelType { get; }
            public Type ProjectionType { get; }
            public Type[] Partitioners { get; }
            public Type QueryHandlerType { get; }
            public Type ResultType { get; }
            public QuerySchema(Type type, Type modelType, Type projectionType, Type queryHandlerType, Type resultType, string category, params Type[] partitioners)
            {
                Type = type;
                ModelType = modelType;
                ProjectionType = projectionType;
                QueryHandlerType = queryHandlerType;
                ResultType = resultType;
                Category = category;
                Partitioners = partitioners;
                _tags = new Lazy<HashSet<string>>(() => new HashSet<string>(Type.FullName.Split('.')));
            }
            private readonly Lazy<HashSet<string>> _tags;
            public IEnumerable<string> Tags => _tags.Value;
            public bool IsTaggedWith(string tag)
            {
                return _tags.Value.Contains(tag);
            }
        }

        public QuerySchemaRegister()
        {
            _querySchema = new List<QuerySchema>();
            _queryTypeIndex = new Dictionary<Type, QuerySchema>();
        }
        public IQuerySchema GetByEventType(Type eventType)
        {
            return _queryTypeIndex[eventType];
        }

        public IEnumerator<IQuerySchema> GetEnumerator()
        {
            return _querySchema.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        class ProjectionInfo
        {
            public Type ProjectionType;
            public Type ModelType;
        }
        public void Discover(IEnumerable<Type> types)
        {
            var queryTypes = types
                .SelectMany(t => t.FindOpenInterfaces(typeof(IQuery<,>)))
                .ToArray();

            var queryHandlerOpenType = typeof(IQueryHandler<,,>);


            var specificImplementations = types
                .Where(x => x.ImplementsOpenInterface(queryHandlerOpenType))
                .Select(x => new
                {
                    HandlerType = x,
                    ImplementedInterfaces = x.FindOpenInterfaces(queryHandlerOpenType)
                });
            var markedWithConventionsImplementations = types
                .WithAttribute<MarkupAttribute>(x => x.ServiceType == queryHandlerOpenType)
                .Select(x => MarkupOpenGenericFactory.Create(x, queryHandlerOpenType).MarkupType)
                .Select(x => new
                {
                    HandlerType = x,
                    ImplementedInterfaces = x.FindOpenInterfaces(queryHandlerOpenType)
                });

            var queryHandlerTypes = specificImplementations.Union(markedWithConventionsImplementations).ToArray();


            var projectionTypes = types
                .Where(t => t.ImplementsOpenInterface(typeof(IProjection<>)) && !t.IsAbstract)
                .ToArray();

            var partitionerTypeIndex = types.Where(x => x.ImplementsOpenInterface(typeof(IProjectionStreamPartitioner<>)) && !x.IsAbstract)
                .ToDictionary(x => x.FindOpenInterfaces(typeof(IProjectionStreamPartitioner<>)).Single());


            List<ProjectionInfo> projectionInfos = new List<ProjectionInfo>();
            foreach (var projectionType in projectionTypes)
            {
                var concreteProjectionTypes = projectionType.FindOpenInterfaces(typeof(IProjection<>)).ToArray();
                foreach (var ct in concreteProjectionTypes)
                {
                    var genericArgs = ct.GetGenericArguments();
                    Type modelType = genericArgs[0];
                    projectionInfos.Add(new ProjectionInfo()
                    {
                        ModelType = modelType,
                        ProjectionType = projectionType
                    });
                }
            }

            foreach (var queryHandlerType in queryHandlerTypes)
            {
                foreach (var queryInterface in queryHandlerType.ImplementedInterfaces)
                {
                    var genericArgs = queryInterface.GetGenericArguments();
                    Type queryType = genericArgs[0];
                    Type modelType = genericArgs[1];
                    Type resultType = genericArgs[2];

                    foreach (var pi in projectionInfos.Where(x => x.ModelType == modelType))
                    {
                        Register(queryType, 
                            modelType, 
                            pi.ProjectionType, 
                            queryHandlerType.HandlerType, 
                            resultType, 
                            partitionerTypeIndex.ContainsKey(pi.ProjectionType) ? new []{ partitionerTypeIndex[pi.ProjectionType]} : Array.Empty<Type>());
                    }
                }
            }

            foreach (var type in queryTypes)
            {
                var genericArgs = type.GetGenericArguments();
                Type modelType = genericArgs[0];
                Type resultType = genericArgs[1];

                if (!_queryTypeIndex.ContainsKey(type))
                    Debug.WriteLine($"Could not find QueryHandler for {type.Name}.");
            }
        }

        private QuerySchema Register(Type type, Type modelType, Type projectionType, Type queryHandlerType, Type resultType, params Type[] partitioners)
        {
            QuerySchema qs = new QuerySchema(type, modelType, projectionType, queryHandlerType, resultType, ServiceConventions.GetCategoryFromNamespaceFunc(queryHandlerType.Namespace), partitioners);
            _querySchema.Add(qs);
            _queryTypeIndex.Add(type, qs);
            return qs;
        }
    }
}