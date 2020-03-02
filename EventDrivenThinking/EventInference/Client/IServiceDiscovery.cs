using System;

namespace EventDrivenThinking.EventInference.Client
{
    public interface IServiceDiscovery
    {
        string Discover(Type commandType);
    }
}