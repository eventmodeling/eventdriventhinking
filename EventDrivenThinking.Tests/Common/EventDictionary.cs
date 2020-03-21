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
        private List<QueryEntry> _queries;
        public class QueryEntry
        {
            public Type QueryType { get; private set; }
            public IQuerySchema QuerySchema { get; private set; }
            public Statement Description { get; private set; }

            public QueryEntry(Type queryType, IQuerySchema querySchema)
            {
                string text = $"{querySchema.Category.Humanize()} {querySchema.Type.Name.Humanize()}";
                QueryType = queryType;
                QuerySchema = querySchema;
                Description = new Statement(text);
            }
        }
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

        private QuerySchemaRegister _querySchemaRegister;
        private AggregateSchemaRegister _aggregateSchemaRegister;
        private CommandInvocationSchemaInvocationSchemaRegister _invocationSchemaRegister;
        private ProjectionSchemaRegister _projectionSchemaRegister;

        public IAggregateSchemaRegister AggregateSchemaRegister => _aggregateSchemaRegister;
        public IQuerySchemaRegister QuerySchemaRegister => _querySchemaRegister;
        public IProjectionSchemaRegister ProjectionSchemaRegister => _projectionSchemaRegister;

        public EventDictionary(params Assembly[] assemblies)
        {
            PrepareCommands(assemblies);
            PrepareEvents(assemblies);
            PrepareQueries(assemblies);
            PrepareProjections(assemblies);
        }

        private void PrepareProjections(Assembly[] assemblies)
        {
            _projectionSchemaRegister = new ProjectionSchemaRegister();
            _projectionSchemaRegister.Discover(assemblies);
        }

        void PrepareQueries(params Assembly[] assemblies)
        {
            _querySchemaRegister = new QuerySchemaRegister();
            _querySchemaRegister.Discover(assemblies);
            _queries = new List<QueryEntry>();

            foreach (var q in _querySchemaRegister)
            {
                _queries.Add(new QueryEntry(q.Type, q));
            }
        }
        void PrepareEvents(params Assembly[] assemblies)
        {
            this._aggregateSchemaRegister = new AggregateSchemaRegister();
            _aggregateSchemaRegister.Discover(assemblies);
            _events = new List<EventEntry>();
            foreach (IAggregateSchema i in _aggregateSchemaRegister)
            {
                foreach (var e in i.Events)
                    _events.Add(new EventEntry(e.EventType, i));
            }
        }
        void PrepareCommands(params Assembly[] assemblies)
        {
            this._invocationSchemaRegister = new CommandInvocationSchemaInvocationSchemaRegister();
            _invocationSchemaRegister.Discover(assemblies);
            _commands = new List<CommandEntry>();
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

        public Type FindQuery(string queryName)
        {
            Statement st = new Statement(queryName);
            var similarEntries = _queries.Select(x => new
                {
                    Entry = x,
                    Similarity = st.ComputeSimilarity(x.Description)
                }).OrderByDescending(x => x.Similarity)
                .ToArray();

            var entry = similarEntries[0].Entry;
            return entry.QueryType;
        }
    }
}