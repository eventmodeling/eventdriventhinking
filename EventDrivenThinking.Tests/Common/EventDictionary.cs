using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventDrivenThinking.EventInference.Schema;
using Humanizer;

namespace EventDrivenThinking.Tests.Common
{
    public class EventDictionary
    {
        private List<EventEntry> _events;
        private List<CommandEntry> _commands;
        public class CommandEntry
        {
            public Type CommandType { get; private set; }
            public IClientCommandSchema CommandSchema { get; private set; }
            public Statement Description { get; private set; }

            public CommandEntry(Type commandType, IClientCommandSchema metadata)
            {
                string text = $"{metadata.Category.Humanize()} {commandType.Name.Humanize()}";
                Description = new Statement(text);
                CommandType = commandType;
                CommandSchema = metadata;
            }
        }
        public class EventEntry
        {
            public Type EventType { get; private set; }
            public IAggregateSchema AggregateSchema { get; private set; }
            public Statement Description { get; private set; }

            public EventEntry(Type eventType, IAggregateSchema schema)
            {
                string text = $"{schema.Category.Humanize()} {eventType.Name.Humanize()}";
                Description = new Statement(text);
                EventType = eventType;
                AggregateSchema = schema;
            }
        }
        private readonly AggregateSchemaRegister _schemaRegister;
        private readonly CommandInvocationSchemaInvocationSchemaRegister _invocationSchemaRegister;
        public IAggregateSchemaRegister SchemaRegister => _schemaRegister;
        public EventDictionary(params Assembly[] assemblies)
        {
            this._schemaRegister = new AggregateSchemaRegister();
            this._invocationSchemaRegister = new CommandInvocationSchemaInvocationSchemaRegister();
            _schemaRegister.Discover(assemblies);
            _invocationSchemaRegister.Discover(assemblies);

            _events = new List<EventEntry>();
            _commands = new List<CommandEntry>();
            foreach (IAggregateSchema i in _schemaRegister)
            {
                foreach(var e in i.Events)
                    _events.Add(new EventEntry(e.EventType, i));
            }
            
            foreach (var c in _invocationSchemaRegister)
                _commands.Add(new CommandEntry(c.Type, c));
        }

        public Type FindEvent(string text)
        {
            Statement st = new Statement(text);
            var similarEntries = _events.Select(x => new
                {
                    Entry = x,
                    Similarity = st.ComputeSimilarity(x.Description)
                }).OrderByDescending(x => x.Similarity)
                .ToArray();

            return similarEntries[0].Entry.EventType;
        }

        public (Type, IClientCommandSchema) FindCommand(string commandType)
        {
            Statement st = new Statement(commandType);
            var similarEntries = _commands.Select(x => new
                {
                    Entry = x,
                    Similarity = st.ComputeSimilarity(x.Description)
                }).OrderByDescending(x => x.Similarity)
                .ToArray();

            var entry = similarEntries[0].Entry;
            return (entry.CommandType, entry.CommandSchema);
        }
    }
}