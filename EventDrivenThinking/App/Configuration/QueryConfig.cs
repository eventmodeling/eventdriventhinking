using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration
{
    public class QueryConfig : SliceStageConfigBase<IQuerySchema>
    {
        public QueryConfig(FeaturePartition partition) : base(partition)
        {
        }

        public override FeaturePartition Register(IServiceCollection serviceCollection)
        {
            foreach (var i in Partition.SchemaRegister.QuerySchema)
            {
                foreach (var p in i.StreamPartitioners)
                    serviceCollection.AddSingleton(typeof(IProjectionStreamPartitioner<>).MakeGenericType(i.ProjectionType), p);

                foreach (var q in i.QueryPartitioners)
                    serviceCollection.AddSingleton(typeof(IQueryPartitioner<>).MakeGenericType(i.Type), q);
            }

            return base.Register(serviceCollection);
        }
    }
}