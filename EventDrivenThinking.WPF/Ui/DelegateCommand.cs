using System;

namespace EventDrivenThinking.Ui
{
    public class DelegateCommand<TArg> : Prism.Commands.DelegateCommand<TArg>
    {
        public DelegateCommand(Action<TArg> executeMethod) : base(executeMethod)
        {
        }

        public DelegateCommand(Action<TArg> executeMethod, Func<TArg, bool> canExecuteMethod) : base(executeMethod,
            canExecuteMethod)
        {
        }
    }
}