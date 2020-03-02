using System;
using System.Collections.Generic;
using System.Reflection;

namespace EventDrivenThinking.EventInference.Schema
{
    
    public interface ICommandInvocationSchemaRegister : ISchemaRegister<IClientCommandSchema>
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
}