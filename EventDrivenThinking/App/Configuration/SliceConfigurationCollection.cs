using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EventDrivenThinking.App.Configuration
{
    public sealed class SliceConfigurationCollection
    {
        private readonly SchemaVisitor _visitor;
        private readonly ILogger _logger;
        private readonly Services _services;
        private readonly List<FeaturePartition> _partitions;

        internal IEnumerable<FeaturePartition> Partitions => _partitions;
        internal SliceConfigurationCollection(ILogger logger, Services services)
        {
            _logger = logger;
            _services = services;
            _visitor = new SchemaVisitor();
            _partitions = new List<FeaturePartition>();
        }
        public FeaturePartition SelectAll()
        {
            bool Filter(ISchema x) => true;
            return SelectByFilter(Filter, "All");
        }
        public FeaturePartition SelectByTag(string tag)
        {
            bool Filter(ISchema x) => x.IsTaggedWith(tag);
            return SelectByFilter(Filter, tag);
        }

        public FeaturePartition SelectByFilter(Predicate<ISchema> filter, string name)
        {
            PartitionSchemaRegister partitionSchema = new PartitionSchemaRegister(name,_visitor,
                filter, _services);

            var feature = new FeaturePartition(partitionSchema, _services, _logger);
            _partitions.Add(feature);
            return feature;
        }

        internal async Task Configure(IServiceProvider sp)
        {
            foreach (var i in _partitions)
            {
                await i.Configure(sp);
            }
        }

        internal void Register(IServiceCollection collection)
        {
            foreach (var i in _partitions)
            {
                i.Register(collection);
            }
        }
    }
}