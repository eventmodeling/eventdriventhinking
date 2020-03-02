using System;
using System.Collections.Generic;
using System.Reflection;

namespace EventDrivenThinking.EventInference.Schema
{

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