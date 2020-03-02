using System;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.EventInference.Client
{
    public class InternalDiscoService : IServiceDiscovery
    {
        private readonly string baseUrl = "http://localhost:5000";

        public string Discover(Type commandType)
        {
            var category = ServiceConventions.GetCategoryFromNamespace(commandType.Namespace);
            var actionName = ServiceConventions.GetActionNameFromCommand(commandType);
            return $"{baseUrl}/{category}/{actionName}";
        }
    }
}