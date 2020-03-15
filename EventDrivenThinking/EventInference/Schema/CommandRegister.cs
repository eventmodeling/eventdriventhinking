using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.CommandHandlers;

namespace EventDrivenThinking.EventInference.Schema
{
    public sealed class CommandInvocationSchemaInvocationSchemaRegister : ICommandInvocationSchemaRegister
    {
        class CommandSchema : IClientCommandSchema
        {
            public Type Type { get; private set; }
            public string Category { get; private set; }
            public bool IsPublic { get; private set; }
            public bool IsCustomCommandHandler { get; private set; }
            public Type CommandHandlerType { get; private set; }
            public CommandSchema(Type commandType, 
                string category, bool isPublic,
                Type handlerType)
            {
                Type = commandType;
                Category = category;
                IsPublic = isPublic;
                IsCustomCommandHandler = handlerType != null;
                CommandHandlerType = handlerType;
                _tags = new Lazy<HashSet<string>>(() => new HashSet<string>(Type.FullName.Split('.')));
            }
            private readonly Lazy<HashSet<string>> _tags;
            public IEnumerable<string> Tags => _tags.Value;
            public bool IsTaggedWith(string tag)
            {
                return _tags.Value.Contains(tag);
            }
        }
        private readonly List<CommandSchema> _commands;
        private Type[] _types;

        public CommandInvocationSchemaInvocationSchemaRegister()
        {
            _commands = new List<CommandSchema>();
        }

        public Type[] Commands
        {
            get { return _types ??= _commands.Select(x=>x.Type).ToArray(); }
        }

        public void Discover(IEnumerable<Type> types)
        {

            foreach (var commandType in types.Where(x => typeof(ICommand).IsAssignableFrom(x) && !x.IsAbstract))
            {
                var customHandler = typeof(ICommandHandler<>).MakeGenericType(commandType);

                var cmdSchema = new CommandSchema(commandType, 
                    GetCategory(commandType), commandType.IsPublic, 
                    types.FirstOrDefault(x=>customHandler.IsAssignableFrom(x)));
                _commands.Add(cmdSchema);
                _types = null;
            }
        }

        public string GetCategory(Type type)
        {
            return ServiceConventions.GetCategoryFromNamespace(type.Namespace);
        }

        IEnumerator<IClientCommandSchema> IEnumerable<IClientCommandSchema>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<IClientCommandSchema> GetEnumerator()
        {
            return _commands.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}