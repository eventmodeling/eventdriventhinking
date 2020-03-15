using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.Contracts;

namespace EventDrivenThinking.Ui.Schema
{
    public class AppProcessSchemaRegister : IAppProcessSchemaRegister
    {
        private readonly List<IAppProcessSchema> _items;

        public AppProcessSchemaRegister()
        {
            _items = new List<IAppProcessSchema>();
        }
        class AppProcessSchema : IAppProcessSchema
        {
            private readonly List<IAppProcessReaction> _reactions;
            public Type Type { get; }
            public string Category { get; }

            public IEnumerable<IAppProcessReaction> Reactions => _reactions;
            
            public void AddReaction(ReactionReason reason, ReactionSource source, Type triggeringType)
            {
                _reactions.Add(new AppProcessReaction(reason, source, triggeringType));
            }
            public AppProcessSchema(Type type, string category)
            {
                Type = type;
                Category = category;
                _reactions = new List<IAppProcessReaction>();
                _tags = new Lazy<HashSet<string>>(() => new HashSet<string>(Type.FullName.Split('.')));
            }
            private readonly Lazy<HashSet<string>> _tags;
            public IEnumerable<string> Tags => _tags.Value;
            public bool IsTaggedWith(string tag)
            {
                return _tags.Value.Contains(tag);
            }
        }
        class AppProcessReaction : IAppProcessReaction
        {
            public AppProcessReaction(ReactionReason reason, ReactionSource source, Type triggeringType)
            {
                Reason = reason;
                Source = source;
                TriggeringType = triggeringType;
            }

            public ReactionReason Reason { get; }
            public ReactionSource Source { get; }
            public Type TriggeringType { get; }
        }

        public IEnumerator<IAppProcessSchema> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// If event's is from assemblies not listed here, it means that it comes from server.
        /// </summary>
        /// <param name="assembly"></param>
        public void Discover(IEnumerable<Type> types)
        {
            var appTypes= types
                .Where(x => typeof(IAppProcess).IsAssignableFrom(x) && !x.IsAbstract)
                .ToArray();

            foreach (var appType in appTypes)
            {
                AppProcessSchema item = Discover(appType, types.Select(x=>x.Assembly).Distinct().ToArray());
                _items.Add(item);
            }
        }
        const BindingFlags DEFAULT_FLAGS = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

        private AppProcessSchema Discover(Type appType, Assembly[] assemblies)
        {
            AppProcessSchema schema = new AppProcessSchema(appType, ServiceConventions.GetCategoryFromNamespace(appType.Namespace));
            var methods = appType.GetMethods(DEFAULT_FLAGS)
                .Where(x=>x.Name == "When")
                .ToArray();

            foreach (var m in methods)
            {
                var args = m.GetParameters();
                if (args.Length == 1)
                {
                    ReactionSource source;
                    ReactionReason reason;
                    var pType = args[0].ParameterType;
                    if (typeof(ICommand).IsAssignableFrom(pType))
                        reason = ReactionReason.Command;
                    else if(typeof(IEvent).IsAssignableFrom(pType))
                        reason = ReactionReason.Event;
                    else reason = ReactionReason.Custom; ;

                    //if (assemblies.Any(x => x == pType.Assembly))
                    if (appType.Assembly == pType.Assembly)
                        source = ReactionSource.UiOnly;
                    else source = ReactionSource.Server;
                    schema.AddReaction(reason, source, pType);
                }
                else if (args.Length == 2)
                {
                    ReactionSource source;
                    ReactionReason reason;
                    var pType = args[1].ParameterType;
                    if (typeof(ICommand).IsAssignableFrom(pType))
                        reason = ReactionReason.Command;
                    else if (typeof(IEvent).IsAssignableFrom(pType))
                        reason = ReactionReason.Event;
                    else reason = ReactionReason.Custom;

                    //if (assemblies.Any(x => x == pType.Assembly))
                    if (appType.Assembly == pType.Assembly)
                        source = ReactionSource.UiOnly;
                    else source = ReactionSource.Server;
                    schema.AddReaction(reason, source, pType);
                }
            }

            return schema;
        }
    }
}
