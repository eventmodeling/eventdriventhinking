using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.CommandHandlers;
using EventDrivenThinking.EventInference.Core;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.Schema
{
    public sealed class AggregateSchemaRegister : IAggregateSchemaRegister
    {
        private readonly Dictionary<Type, AggregateSchema> _aggregates;
        private readonly Dictionary<Type, AggregateSchema> _commandIndex;
        private readonly Dictionary<Type, AggregateSchema> _eventIndex;
        private readonly List<CommandSchema> _commands;
        private readonly List<AggregateEventSchema> _events;

        public AggregateSchemaRegister()
        {
            _commands = new List<CommandSchema>();
            _events = new List<AggregateEventSchema>();
            _commandIndex = new Dictionary<Type, AggregateSchema>();
            _eventIndex = new Dictionary<Type, AggregateSchema>();
            _aggregates = new Dictionary<Type, AggregateSchema>();
        }

        public IAggregateSchema FindAggregateByCommand<TCommand>() where TCommand : ICommand
        {
            var commandType = typeof(TCommand);
            return FindAggregateByCommand(commandType);
        }

        public IAggregateSchema FindAggregateByCommand(Type commandType)
        {
            return _commandIndex[commandType];
        }

        public IAggregateSchema FindAggregateByEvent<TEvent>() where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            return FindAggregateByEvent(eventType);
        }

        public IAggregateSchema FindAggregateByEvent(Type eventType)
        {
            return _eventIndex[eventType];
        }

        public IEnumerable<Type> GetEvents<TAggregate>()
        {
            return GetEvents(typeof(TAggregate));
        }

        public IEnumerable<Type> GetEvents(Type aggregateType)
        {
            var meta = Get(aggregateType);
            if (meta != null)
                return meta.Events.Select(x=>x.EventType);
            return Array.Empty<Type>();
        }

        public IEnumerable<Type> GetCommands<TAggregate>()
        {
            return GetCommands(typeof(TAggregate));
        }

        public IEnumerable<Type> GetCommands(Type aggregateType)
        {
            var meta = Get(aggregateType);
            if (meta != null)
                return meta.Commands.Select(x=>x.Type);
            return Array.Empty<Type>();
        }

        public IAggregateSchema Get<TAggregate>()
        {
            return Get(typeof(TAggregate));
        }

        public IAggregateSchema Get(Type aggregate)
        {
            return _aggregates[aggregate];
        }

        public void Discover(params Assembly[] assemblies)
        {
            foreach (var a in assemblies)
            {
                var types = a.GetTypes();
                var aggregateTypes = types
                    .Where(t => typeof(IAggregate).IsAssignableFrom(t) && !t.IsAbstract)
                    .ToArray();

                foreach (var type in aggregateTypes)
                {
                    var metadata = new AggregateSchema(type);
                    _aggregates.Add(type, metadata);
                    DiscoverWhens(metadata);
                    DiscoverGivens(metadata);
                }

                var events = types.Where(x => typeof(IEvent).IsAssignableFrom(x) && !x.IsAbstract)
                    .ToArray();

                foreach (var eventType in events)
                {
                    if (!_eventIndex.ContainsKey(eventType))
                    {
                        string category = ServiceConventions.GetCategoryFromNamespace(eventType.Namespace);
                        var aggregate = _aggregates.Values.FirstOrDefault(x => x.Category == category);
                        if (aggregate != null)
                        {
                            var isOwnedByAggregate = aggregate.Type.Namespace == eventType.Namespace;
                            AggregateEventSchema es = new AggregateEventSchema(eventType, false, isOwnedByAggregate);
                            aggregate.Events.Add(es);
                        }
                    }
                }

                var commands = types.Where(x => typeof(ICommand).IsAssignableFrom(x) && !x.IsAbstract).ToArray();

                foreach (var command in commands.Where(x => !_commandIndex.ContainsKey(x)))
                {
                    var customHandler = typeof(ICommandHandler<>).MakeGenericType(command);
                    var handler = types.FirstOrDefault(x => customHandler.IsAssignableFrom(x));
                    if(handler != null)
                        _commands.Add(new CommandSchema(command, 
                            ServiceConventions.GetCategoryFromNamespace(command.Namespace), 
                            command.IsPublic, true, handler));
                    else 
                        Debug.WriteLine($"Found a dangling command: {command.FullName}");
                }
            }
        }

        public string GetCategory(Type command)
        {
            return _commandIndex[command].Category;
        }

        public Type[] Events
        {
            get
            {
                return _events.Select(x=>x.EventType).ToArray();
            }
        }

        public Type[] Commands
        {
            get { return _commands.Select(x => x.Type).ToArray(); }
        }
        class AggregateEventSchema : IAggregateEventSchema
        {
            public AggregateEventSchema(Type eventType, bool changesState, bool isOwnedByAggregate)
            {
                ChangesState = changesState;
                EventType = eventType;
                IsOwnedByAggregate = isOwnedByAggregate;
            }

            /// <summary>
            /// Indicates whether Given method is implemented at the aggregate
            /// </summary>
            public bool ChangesState { get; private set; }
            public Type EventType { get; private set; }
            /// <summary>
            /// Indicates whether the Aggregate emits this event, or weather it is owned by other aggregate and
            /// only linked to this aggregate's stream. It's useful when we want to start life on an new aggregate
            /// base on same event. Auto-wiring projections should be constructed in the background to make this happen.
            /// BE-AWARE, no optimistic concurrency check can be performed, so this event should be only used for
            /// starting a new aggregate stream.
            /// </summary>
            public bool IsOwnedByAggregate { get; private set; }
        }
        class CommandSchema : ICommandSchema
        {
            public CommandSchema(Type type, string category, 
                bool isPublic, bool isCustomCommandHandler, Type handlerType = null, Type[] events = null)
            {
                Type = type;
                Category = category;
                IsPublic = isPublic;
                IsCustomCommandHandler = isCustomCommandHandler;
                this.CommandHandlerType = handlerType;
                if (events == null) Events = Array.Empty<Type>();
                else Events = events;
                _tags = new Lazy<HashSet<string>>(() => new HashSet<string>(Type.FullName.Split('.')));
            }
            private readonly Lazy<HashSet<string>> _tags;
            public IEnumerable<string> Tags => _tags.Value;
            public bool IsTaggedWith(string tag)
            {
                return _tags.Value.Contains(tag);
            }
            public Type[] Events { get; private set; }
            public Type Type { get; private set; }
            public string Category { get; private  set; }
            public bool IsPublic { get; private  set; }
            public bool IsCustomCommandHandler { get; private set; }
            public Type CommandHandlerType { get; private set; }
        }

        public IEnumerator<IAggregateSchema> GetEnumerator()
        {
            return _aggregates.Select(x => x.Value).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerable<MethodInfo> GetWhensMethods(Type t)
        {
            var whens = t.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            return whens.Where(x => x.Name == "When"
                                    && x.GetParameters().Length == 2
                                    && typeof(ICommand).IsAssignableFrom(x.GetParameters()[1].ParameterType));
        }

        private IEnumerable<MethodInfo> GetGivensMethods(Type t)
        {
            var whens = t.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            return whens.Where(x => x.Name == "Given"
                                    && x.GetParameters().Length == 2
                                    && typeof(IEvent).IsAssignableFrom(x.GetParameters()[1].ParameterType));
        }

        private void DiscoverWhens(AggregateSchema type)
        {
            foreach (var whenMethod in GetWhensMethods(type.Type))
            {
                var cmdType = whenMethod.GetParameters()[1].ParameterType;
                var category = ServiceConventions.GetCategoryFromNamespace(cmdType.Namespace);
                if (category != type.Category)
                    throw new CategoryMismatchByNamespaceException(cmdType, type.Type);

                Type[] events = Array.Empty<Type>();
                if (typeof(IEvent).IsAssignableFrom(whenMethod.ReturnType) && whenMethod.ReturnType.IsClass)
                    events = new Type[] { whenMethod.ReturnType };
                else
                {
                    var attribute = whenMethod.GetCustomAttribute<EmittingEventsAttribute>();
                    if (attribute != null)
                        events = attribute.EventTypes;
                }
                var commandSchema = new CommandSchema(cmdType, category, cmdType.IsPublic, false, null, events);
                _commands.Add(commandSchema);
                type.Commands.Add(commandSchema);
                _commandIndex.TryAdd(cmdType, type);
            }
        }

        private void DiscoverGivens(AggregateSchema type)
        {
            foreach (var givenMethod in GetGivensMethods(type.Type))
            {
                var eventType = givenMethod.GetParameters()[1].ParameterType;
                if (givenMethod.ReturnType != type.StateType)
                    throw new InvalidAggregateSchemaException(type.Type, eventType);

                var isOwnedByAggregate = eventType.Namespace == type.Type.Namespace;
                
                AggregateEventSchema es = new AggregateEventSchema(eventType, true, isOwnedByAggregate);
                type.Events.Add(es);
                _events.Add(es);
                _eventIndex.TryAdd(eventType, type);
            }

        }

        
        private class AggregateSchema : IAggregateSchema
        {
            
            public readonly List<CommandSchema> Commands;
            public readonly List<AggregateEventSchema> Events;

            public AggregateSchema(Type type)
            {
                Type = type;
                Commands = new List<CommandSchema>();
                Events = new List<AggregateEventSchema>();
                StateType = FindState(type.BaseType);
                _tags = new Lazy<HashSet<string>>(() => new HashSet<string>(Type.FullName.Split('.')));
            }
            private readonly Lazy<HashSet<string>> _tags;
            public IEnumerable<string> Tags => _tags.Value;
            public bool IsTaggedWith(string tag)
            {
                return _tags.Value.Contains(tag);
            }

            private Type FindState(Type baseType)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(Aggregate<>))
                {
                    return baseType.GetGenericArguments()[0];
                }
                if(baseType.BaseType == typeof(object))
                    throw new InvalidOperationException("Cannot find state type fo aggregate.");
                return FindState(baseType.BaseType);
            }

            public string Category => ServiceConventions.GetCategoryFromNamespace(Type.Namespace);

            public Type Type { get; }
            public Type StateType { get; }
            IEnumerable<ICommandSchema> IAggregateSchema.Commands => Commands;

            IEnumerable<IAggregateEventSchema> IAggregateSchema.Events => Events;
            public IAggregateEventSchema EventByName(string eventEventType)
            {
                return Events.FirstOrDefault(x => x.EventType.Name == eventEventType || x.EventType.FullName == eventEventType);
            }

        }
    }

    public class InvalidAggregateSchemaException : Exception
    {
        public Type AggregateType { get; }
        public Type EventType { get; }

        public InvalidAggregateSchemaException(Type aggregateType, Type eventType)
        {
            AggregateType = aggregateType;
            EventType = eventType;
        }
    }
    public class CategoryMismatchByNamespaceException : Exception {
        public Type CommandType { get; }
        public Type AggregateType { get; }

        public CategoryMismatchByNamespaceException(Type commandType, Type aggregateType)
        {
            CommandType = commandType;
            AggregateType = aggregateType;
        }
    }
}