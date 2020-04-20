using System;
using System.Collections.Generic;
using System.Text;

using EventDrivenThinking.App.Configuration.SignalR;

namespace EventDrivenThinking.App.Configuration
{
    public static class SignalRConfigExtensions
    {
        public static FeaturePartition SubscribeFromSignalR(this ProjectionsConfig config, string url)
        {
            return config.Merge(new ProjectionsSliceStartup(url));
        }
        public static FeaturePartition SubscribeFromSignalR(this ProcessorsConfig config, string url)
        {
            return config.Merge(new ProcessorsSliceStartup(url));
        }

    }
}
