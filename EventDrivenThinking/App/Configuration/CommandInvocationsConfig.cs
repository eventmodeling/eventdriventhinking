using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.App.Configuration
{
    public class CommandsConfig: SliceStageConfigBase<IClientCommandSchema>
    {
        internal CommandsConfig(FeaturePartition featurePartition) : base(featurePartition)
        {
            
        }

    }

    

    public class EventsConfig : SliceStageConfigBase<IEventSchema>
    {
        public EventsConfig(FeaturePartition partition) : base(partition)
        {
        }
    }
}