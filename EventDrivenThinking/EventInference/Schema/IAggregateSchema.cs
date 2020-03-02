using System;
using System.Collections.Generic;
using System.Reflection;

namespace EventDrivenThinking.EventInference.Schema
{
    public interface IAggregateSchema : ISchema
    {
        IEnumerable<ICommandSchema> Commands { get; }
        IEnumerable<IAggregateEventSchema> Events { get; }
        IAggregateEventSchema EventByName(string eventEventType);

    }

    public interface ISchemaRegister<T> : ISchemaRegister,IEnumerable<T>
        where T:ISchema
    {
        
    }

    public interface ISchemaRegister
    {
        void Discover(params Assembly[] assemblies);
    }
    public interface ISchema
    {
        Type Type { get; }
        
        string Category { get; }
        IEnumerable<string> Tags { get; }
        bool IsTaggedWith(string tag);
    }
}