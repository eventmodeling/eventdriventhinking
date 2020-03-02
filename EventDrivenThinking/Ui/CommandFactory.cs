using System;
using System.Diagnostics;
using System.Reflection;
using CommonServiceLocator;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Core;
using Prism.Commands;
using Serilog;

namespace EventDrivenThinking.Ui
{
    public class CommandFactory<TViewModel, TKey, TCommand> : ICommandFactory<TViewModel>
        where TCommand : ICommand
    {
        private readonly IEventPublisher<CommandEnvelope<TKey, TCommand>> _uiEvent;
        private Func<TViewModel, (TKey, TCommand)> _action;
        private ILogger _logger;
        public CommandFactory()
        {
            _uiEvent = ServiceLocator.Current.GetInstance<IUiEventBus>()
                .GetEvent<CommandEnvelope<TKey, TCommand>>();
            _logger = ServiceLocator.Current.GetInstance<ILogger>();
        }

        public System.Windows.Input.ICommand Create(TViewModel vm)
        {
            return new DelegateCommand(() =>
            {
                var args = _action(vm);
                var cmd = new CommandEnvelope<TKey, TCommand>(args.Item1, args.Item2);

                _uiEvent.Publish(cmd);
                _logger.Information("Invoked command {commandName} though eventPublisher", typeof(TCommand).Name);
            });
        }

        private string _commandName;
        public string CommandName => _commandName ?? typeof(TCommand).Name;

        public void Configure(MethodInfo minfo)
        {
            var customName = minfo.GetCustomAttribute<CommandNameAttribute>();
            if (customName != null)
                _commandName = customName.Name;

            _action = (Func<TViewModel, (TKey, TCommand)>) minfo.CreateDelegate(
                typeof(Func<TViewModel, (TKey, TCommand)>));
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandNameAttribute : Attribute
    {
        public CommandNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}