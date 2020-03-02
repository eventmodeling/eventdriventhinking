﻿using System.Collections.Generic;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.App.Configuration.Fresh
{
    public interface IPartitionSchemaRegister
    {
        string PartitionName { get; }
        IEnumerable<IAggregateSchema> AggregateSchema { get; }
        IEnumerable<IProjectionSchema> ProjectionSchema { get; }
        IEnumerable<IProcessorSchema> ProcessorSchema { get; }
        
        IEnumerable<IClientCommandSchema> CommandInvocationSchema { get; }
        IEnumerable<T> Set<T>() where T : ISchema;
        //IPartitionSchemaRegister AddSchema<T>(IEnumerable<T> items)
        //    where T : ISchema;
    }
}