﻿using System;
using System.Linq;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.CommandHandlers;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EventDrivenThinking.App.Configuration
{
    public class AggregateConfig : SliceStageConfigBase<IAggregateSchema>
    {
        
        internal AggregateConfig(FeaturePartition partition) : base(partition)
        {
        }

        public override FeaturePartition Register(IServiceCollection collection)
        {
            foreach (var i in Partition.SchemaRegister.AggregateSchema)
            {
                var at = i.Type;
                collection.TryAddSingleton(typeof(IEventMetadataFactory<>).MakeGenericType(at), typeof(EventMetadataFactory<>).MakeGenericType(at)); // need to register them manually!

                var agSchemaSpecType = typeof(AggregateSchema<>).MakeGenericType(at);
                object agSchemaInstance = Activator.CreateInstance(agSchemaSpecType, i);
                collection.AddSingleton(typeof(IAggregateSchema<>).MakeGenericType(at), agSchemaInstance);

                foreach (var c in i.Commands)
                {
                    Type[] args = new[] { at, c.Type };
                    var implementationType = typeof(AggregateCommandHandler<,>).MakeGenericType(args);
                    collection.TryAddScoped(typeof(ICommandHandler<>).MakeGenericType(c.Type),
                        implementationType);
                    collection.AddScoped(implementationType);
                }

                Partition.Logger.Information("Discovered {aggregateSchema}", new
                {
                    Catalog = i.Category,
                    Commands = String.Join(",", i.Commands.Select(x => x.Type.Name)),
                    Events = String.Join(",", i.Events.Select(x => x.EventType.Name))
                });
            }
            return base.Register(collection);
        }
    }
}