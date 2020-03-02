using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenThinking.EventInference.Schema
{
    public interface IAggregateSchemaRegister : ISchemaRegister<IAggregateSchema>
    {
        Type[] Events { get; }
        IAggregateSchema FindAggregateByCommand<TCommand>() where TCommand : ICommand;
        IAggregateSchema FindAggregateByCommand(Type commandType);

        IAggregateSchema FindAggregateByEvent<TEvent>() where TEvent : IEvent;
        IAggregateSchema FindAggregateByEvent(Type eventType);

        IEnumerable<Type> GetEvents<TAggregate>();
        IEnumerable<Type> GetEvents(Type aggregateType);

        IEnumerable<Type> GetCommands<TAggregate>();
        IEnumerable<Type> GetCommands(Type aggregateType);

        IAggregateSchema Get<TAggregate>();
        IAggregateSchema Get(Type aggregate);
        
    }

    public interface IAggregateEventSchema
    {
        bool ChangesState { get; }
        Type EventType { get; }
        bool IsOwnedByAggregate { get; }
    }
    public interface IAggregateSchema<T> : IAggregateSchema
        where T : IAggregate
    {
        
    }
}