using System;
using System.Collections.Generic;
using System.Linq;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.App.Configuration
{
    class PartitionSchemaRegister : IPartitionSchemaRegister
    {
        private readonly SchemaVisitor _visitor;
        private readonly Predicate<ISchema> _filter;
        private readonly Services _services;
        private readonly string _partitionName;
        
        public string PartitionName => _partitionName;

        private readonly Dictionary<Type, ISchema[]> _schemas;

        public override string ToString()
        {
            return _partitionName;
        }

        public PartitionSchemaRegister(string partitionName, 
            SchemaVisitor visitor,
            Predicate<ISchema> filter, 
            Services services)
        {
            _partitionName = partitionName;
           
            _visitor = visitor;
            _filter = filter;
            _services = services;
            _schemas = new Dictionary<Type, ISchema[]>();

            AddSchema(services.GetSchemaRegister<IAggregateSchema>())
                .AddSchema(services.GetSchemaRegister<IProjectionSchema>())
                .AddSchema(services.GetSchemaRegister<IProcessorSchema>())
                .AddSchema(services.GetSchemaRegister<IClientCommandSchema>());
            
        }
        
        private PartitionSchemaRegister AddSchema<T>(IEnumerable<T> items)
            where T:ISchema
        {
            _schemas.Add(typeof(T), items.Cast<ISchema>().ToArray());
            return this;
        }

        public IEnumerable<IAggregateSchema> AggregateSchema
        {
            get
            {
                foreach (var i in Set<IAggregateSchema>().Where(x => _filter(x)))
                {
                    _visitor.VisitOnce(i, this);
                    yield return i;
                }
            }
        }

        public IEnumerable<IProjectionSchema> ProjectionSchema
        {
            get
            {
                foreach (var i in Set<IProjectionSchema>().Where(x => _filter(x)))
                {
                    _visitor.VisitOnce(i, this);
                    yield return i;
                }
            }
        }

        public IEnumerable<IProcessorSchema> ProcessorSchema
        {
            get
            {
                foreach (var i in Set<IProcessorSchema>().Where(x => _filter(x)))
                {
                    _visitor.VisitOnce(i, this);
                    yield return i;
                }
            }
        }
        public IEnumerable<IClientCommandSchema> CommandInvocationSchema
        {
            get
            {
                foreach (var i in Set<IClientCommandSchema>().Where(x => _filter(x)))
                {
                    _visitor.VisitOnce(i, this);
                    yield return i;
                }
            }
        }

        public IEnumerable<T> Set<T>() where T:ISchema
        {
            var register = _services.GetSchemaRegister<T>();
            foreach (var i in register.Where(x=>_filter(x)))
            {
                _visitor.VisitOnce(i, this);
                yield return i;
            }
        }
    }
}