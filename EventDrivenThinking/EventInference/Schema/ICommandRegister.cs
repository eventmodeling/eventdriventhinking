using System;
using System.Collections.Generic;
using System.Reflection;

namespace EventDrivenThinking.EventInference.Schema
{
    public interface IEventSchemaRegister : ISchemaRegister<IEventSchema>
    {

    }
    public interface ICommandsSchemaRegister : ISchemaRegister<IClientCommandSchema>
    {
        Type[] Commands { get; }
        string GetCategory(Type command);
    }

    public interface IClientCommandSchema : ISchema
    {
        Type Type { get; }
    }
    public interface ICommandSchema : IClientCommandSchema
    {
        bool IsPublic { get;}
        bool IsCustomCommandHandler { get;  }
        Type CommandHandlerType { get; }
    }
    
    /// <summary>
    /// This schema is used for creating projection streams
    /// Maybe we should change it.
    /// It seems as we just need to configure projections differently?
    /// </summary>
    public interface IEventSchema : ISchema
    {
        // a very bold move to include projection schema. 
        // since it increases coupling :-(
        IEnumerable<IProjectionSchema> Projections { get; }
    }

    
}