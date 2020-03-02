using System;

namespace EventDrivenThinking.App.Configuration
{
    public class ConfigurationTask : IConfigurationTask
    {
        private readonly Action _action;

        public ConfigurationTask(Action action, int order)
        {
            _action = action;
            Order = order;
        }

        public int Order { get; }

        public void Run()
        {
            _action();
        }
    }
}