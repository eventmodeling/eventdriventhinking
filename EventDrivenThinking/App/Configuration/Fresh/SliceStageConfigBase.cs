using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.App.Configuration.Fresh.Carter;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.Fresh
{
    public interface IStageConfig
    {
        Task<FeaturePartition> Configure(IServiceProvider provider);
        FeaturePartition Register(IServiceCollection collection);
    }
    public abstract class SliceStageConfigBase<T> : IStageConfig
        where T:ISchema
    {
        private readonly FeaturePartition _partition;
        private readonly IList<ISliceStartup> _configs;
        protected FeaturePartition Partition => _partition;

        public FeaturePartition Merge(ISliceStartup<T> startup)
        {
            _configs.Add(startup);
            startup.Initialize(Partition.SchemaRegister.Set<T>());

            return Partition;
        }
        protected SliceStageConfigBase(FeaturePartition partition)
        {
            _partition = partition;
            _configs = new List<ISliceStartup>();
        }

        public virtual async Task<FeaturePartition> Configure(IServiceProvider provider)
        {
            if (_configs.Any())
            {
                foreach(var config in _configs)
                    await config.ConfigureServices(provider);
                
            }
            
            return _partition;
        }

        public virtual FeaturePartition Register(IServiceCollection collection)
        {
            foreach (var config in _configs)
            {
                config.RegisterServices(collection);
                var extensionConfig = config as IServiceExtensionConfigProvider;
                extensionConfig?.Register(Partition.ServiceExtensionProvider);
            }

            return _partition;
        }
    }
}