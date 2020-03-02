using System;
using System.Collections.Generic;
using System.Reflection;
using EventDrivenThinking.App.Configuration.Server;
using EventDrivenThinking.EventInference.Schema;
using Unity;

namespace EventDrivenThinking.App.Configuration.Client
{
    public class ClientPipelineBuilder : IPipelineBuilder
    {
        private readonly List<SendCommandSyntax> _items;
        private readonly List<Assembly> _assemblies;
        public void Build()
        {
            ConnectPipes();
        }

        public void RegisterForDiscovery(params Assembly[] assemblies)
        {
            _assemblies.AddRange(assemblies);
        }

        
        private void ConnectPipes()
        {
            // before we connect we need to check if things are not messy.
            foreach (var i in _items)
            {
                i.Build();
            }
        }

        private readonly IUnityContainer _container;

        public ClientPipelineBuilder(IUnityContainer container)
        {
            _container = container;
            _items = new List<SendCommandSyntax>();
            _assemblies = new List<Assembly>();
        }
        public SendCommandSyntax Slices(Predicate<ISchema> schemaFilter = null)
        {
            if (schemaFilter == null) schemaFilter = x => true;
            var receiveSyntax = new SendCommandSyntax(_container.Resolve<IServiceProvider>(), schemaFilter);
            _items.Add(receiveSyntax);
            return receiveSyntax;
        }
        public T Resolve<T>()
        {
            return _container.Resolve<T>();
        }
    }
}