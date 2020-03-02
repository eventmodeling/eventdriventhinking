using EventDrivenThinking.App.Configuration.Server;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.Fresh
{
    public class CommandInvocationsConfig: SliceStageConfigBase<IClientCommandSchema>
    {
        internal CommandInvocationsConfig(FeaturePartition featurePartition) : base(featurePartition)
        {
            
        }

    }
}