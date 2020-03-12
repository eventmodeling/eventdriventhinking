using System;
using System.Collections.Generic;
using System.Reflection;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Schema
{

    public interface IQuerySchemaRegister
    {
        IQuerySchema GetByEventType(Type eventType);
    }

    public interface IQueryPartitioner<in TQuery>
    {
        Guid CalculatePartition(IModel model, TQuery query);
    }
    public interface IProjectionPartitioner<in TProjection>
    {
        Guid CalculatePartition(IModel model, EventMetadata m, IEvent ev);
    }

    public interface IQuerySchema
    {
        public Type QueryType { get; set; }
        public Type ModelType { get; set; }
        public Type ProjectionType { get; set; }
    }

    public interface IProjectionSchemaRegister : ISchemaRegister<IProjectionSchema>
    {
        Type[] Events { get; }
        IProjectionSchema FindByEvent(Type eventType);
        IProjectionSchema FindByModelType(Type modelType);
        
    }

    public interface IProcessorSchemaRegister : ISchemaRegister<IProcessorSchema>
    {
        Type[] Events { get; }
        IProcessorSchema FindByEvent(Type eventType);
        
    }
    public interface IProcessorSchema<TProcessor> : IProcessorSchema { }
    public interface IProcessorSchema : ISchema
    {
        IEnumerable<Type> Events { get; }
        Type Type { get; }
    }

    public interface IProjectionSchema<TProjection> : IProjectionSchema
    {
    }

    public interface IProjectionSchema : ISchema
    {
        IEnumerable<Type> Events { get; }
        Type Type { get; }
        Type ModelType { get; }
    }
}