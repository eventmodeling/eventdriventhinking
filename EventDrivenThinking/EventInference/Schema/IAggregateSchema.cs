using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

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
        void Discover(IEnumerable<Type> types);
    }
    public interface ISchema
    {
        Type Type { get; }
        
        string Category { get; }
        IEnumerable<string> Tags { get; }
        bool IsTaggedWith(string tag);
    }
    public static class SchemaRegisterExtensions {

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TSchemaRegister Discover<TSchemaRegister>(this TSchemaRegister register)
            where TSchemaRegister : ISchemaRegister
        {
            var caller =  Assembly.GetCallingAssembly();
            register.Discover(caller.GetTypes());

            return register;
        }
        
        public static TSchemaRegister Discover<TSchemaRegister>(this TSchemaRegister register,
            params Assembly[] assemblies)
            where TSchemaRegister : ISchemaRegister
        {
            register.Discover(assemblies.SelectMany(x => x.GetTypes()));
            return register;
        }
    }
}