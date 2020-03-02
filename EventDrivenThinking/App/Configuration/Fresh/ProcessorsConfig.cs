using System;
using EventDrivenThinking.App.Configuration.Server;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.EventHandlers;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.Fresh
{
    public class ProcessorsConfig : SliceStageConfigBase<IProcessorSchema>
    {
        
        internal ProcessorsConfig(FeaturePartition partition) : base(partition)
        {
        }

        public override FeaturePartition Register(IServiceCollection collection)
        {
            foreach (var p in Partition.SchemaRegister.ProcessorSchema)
            {
                collection.AddScoped(p.Type);

                collection.AddSingleton(typeof(IProcessorSchema<>).MakeGenericType(p.Type),
                    Activator.CreateInstance(typeof(ProcessorSchema<>).MakeGenericType(p.Type), p));

                foreach (var et in p.Events)
                {
                    Type handlerProcessType = typeof(ProcessorEventHandler<,>).MakeGenericType(p.Type, et);
                    Type handlerInterfaceType = typeof(IEventHandler<>).MakeGenericType(et);

                    collection.AddScoped(handlerInterfaceType, handlerProcessType);
                    Partition.Logger.Information("Discovered event handler {processorName} for {eventName}", p.Type.Name, et.Name);
                }
            }
            return base.Register(collection);
        }
    }
}