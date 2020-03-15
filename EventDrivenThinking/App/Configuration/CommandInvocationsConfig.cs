using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.App.Configuration
{
    public class CommandInvocationsConfig: SliceStageConfigBase<IClientCommandSchema>
    {
        internal CommandInvocationsConfig(FeaturePartition featurePartition) : base(featurePartition)
        {
            
        }

    }
}