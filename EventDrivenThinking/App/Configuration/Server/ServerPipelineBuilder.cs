using System;
using System.Collections.Generic;
using System.Reflection;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.Server
{
    public interface IPipelineBuilder
    {
        void Build();
    }
   
    public class ServerPipelineBuilder : IPipelineBuilder
    {
        private readonly List<SendPipeDescription> _items;
        private readonly List<Assembly> _assemblies;
        private readonly IServiceProvider _serviceProvider;

        public SendPipeDescription Slices(Predicate<ISchema> categoryFilter = null)
        {
            IServiceCollection s = new ServiceCollection();
            
            if (categoryFilter == null) categoryFilter = x => true;
            var receiveSyntax = new SendPipeDescription(_serviceProvider, categoryFilter);
            _items.Add(receiveSyntax);
            return receiveSyntax;
        }
       
        public void Build()
        {
            ConnectPipes();
        }


        private void ConnectPipes()
        {
            foreach (var i in _items)
            {
                i.Build();
            }
        }


        public ServerPipelineBuilder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _assemblies = new List<Assembly>();
            _items = new List<SendPipeDescription>();
        }

    }
}