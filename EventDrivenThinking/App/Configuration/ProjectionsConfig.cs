using System;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration
{
    public class ProjectionsConfig : SliceStageConfigBase<IProjectionSchema>
    {
        internal ProjectionsConfig(FeaturePartition partition) : base(partition)
        {
        }

        private bool _noHandlers;
        public ProjectionsConfig NoGlobalHandlers()
        {
            _noHandlers = true;
            return this;
        }

        public override FeaturePartition Register(IServiceCollection collection)
        {
            foreach (var p in Partition.SchemaRegister.ProjectionSchema)
            {
                collection.AddScoped(p.Type);

                collection.AddSingleton(typeof(IProjectionSchema<>).MakeGenericType(p.Type),
                    Activator.CreateInstance(typeof(ProjectionSchema<>).MakeGenericType(p.Type), p));

                foreach (var pp in p.Partitioners)
                    collection.AddSingleton(typeof(IProjectionStreamPartitioner<>).MakeGenericType(p.Type), pp);

                if(!_noHandlers)
                foreach (var et in p.Events)
                {
                    Type handlerProjectionType = typeof(ProjectionEventHandler<,>).MakeGenericType(p.Type, et);
                    Type handlerInterfaceType = typeof(IEventHandler<>).MakeGenericType(et);
                    collection.AddScoped(handlerInterfaceType, handlerProjectionType);
                    Partition.Logger.Information("Discovered event handler {projectionName} for {eventName}", p.Type.Name, et.Name);
                }
            }

            return base.Register(collection);
        }
    }

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