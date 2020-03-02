using System;
using EventDrivenThinking.App.Configuration.Client;
using EventDrivenThinking.App.Configuration.Server;
using Unity;

namespace EventDrivenThinking.Integrations.Unity.Client
{
    public static class UnityExtensions
    {
        public static IPipelineBuilder ConfigureClient(this IUnityContainer container, Action<ClientPipelineBuilder> opt)
        {
            var c = new ClientPipelineBuilder(container);
            opt(c);

            return c;
        }
    }
    
}
