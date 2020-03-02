using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.App.Configuration.Server;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace EventDrivenThinking.App.Configuration.Fresh
{
    public class FeaturePartition
    {
        private readonly Lazy<AggregateConfig> _aggregates;
        private readonly Lazy<ProjectionsConfig> _projections;
        private readonly Lazy<ProcessorsConfig> _processors;
        private readonly Lazy<CommandInvocationsConfig> _commandInvocations;
        internal IPartitionSchemaRegister SchemaRegister { get; }
        internal IServiceExtensionProvider ServiceExtensionProvider { get; }
        internal ILogger Logger { get; }
        internal IList<IStageConfig> Configs { get; }
        internal FeaturePartition(IPartitionSchemaRegister schemaRegister, 
            IServiceExtensionProvider serviceExtensionProvider, 
            ILogger logger)
        {
            SchemaRegister = schemaRegister;
            this.ServiceExtensionProvider = serviceExtensionProvider;
            Logger = logger.ForContext("FeatureName",schemaRegister.PartitionName);
            _aggregates = new Lazy<AggregateConfig>(() => WriteThrough(new AggregateConfig(this)));
            _processors = new Lazy<ProcessorsConfig>(() => WriteThrough(new ProcessorsConfig(this)));
            _projections = new Lazy<ProjectionsConfig>(() => WriteThrough(new ProjectionsConfig(this)));
            _commandInvocations = new Lazy<CommandInvocationsConfig>(() => WriteThrough(new CommandInvocationsConfig(this)));
            Configs = new List<IStageConfig>();
        }

        private T WriteThrough<T>(T config) where T : IStageConfig
        {
            Configs.Add(config);
            return config;
        }

        public T ComponentConfig<T>() where T:IStageConfig
        {
            var component = Configs.OfType<T>().FirstOrDefault();
            if (component == null)
            {
                component = (T) Activator.CreateInstance(typeof(T), this);
                Configs.Add(component);
            }

            return component;
        }
        internal void Register(IServiceCollection collection)
        {
            foreach (var i in Configs)
                i.Register(collection);
        }
        internal async Task Configure(IServiceProvider sp)
        {
            foreach (var i in Configs)
                await i.Configure(sp);
        }

        public AggregateConfig Aggregates => _aggregates.Value;

        public ProjectionsConfig Projections => _projections.Value;

        public ProcessorsConfig Processors => _processors.Value;

        public CommandInvocationsConfig CommandInvocations => _commandInvocations.Value;
    }
}