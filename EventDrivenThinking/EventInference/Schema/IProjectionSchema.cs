using System;
using System.Collections.Generic;
using System.Reflection;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Schema
{

    public interface IQuerySchemaRegister : ISchemaRegister<IQuerySchema>
    {
        IQuerySchema GetByQueryType(Type queryType);
    }

    public interface IQueryPartitioner<in TQuery>
    {
        Guid CalculatePartition(IModel model, TQuery query);
    }

    public interface IProjectionStreamPartitioner<in TProjection> where TProjection:IProjection
    {
        Guid[] CalculatePartitions(IModel model, EventMetadata m, IEvent ev);
    }


    public interface IQuerySchema : ISchema
    {
        public Type ModelType { get; }
        public Type ProjectionType { get; }
        public Type[] StreamPartitioners { get; }
        public Type[] QueryPartitioners { get; }
        public Type ResultType { get; }
        public Type QueryHandlerType { get; }
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
    }

    public interface IProjectionSchema<TProjection> : IProjectionSchema
    {
    }

    public interface IProjectionSchema : ISchema
    {
        Guid ProjectionHash { get; }
        IEnumerable<Type> Events { get; }
        Type ModelType { get; }
        Type EventByName(string eventEventType);
        IEnumerable<Type> Partitioners { get; }
    }
}