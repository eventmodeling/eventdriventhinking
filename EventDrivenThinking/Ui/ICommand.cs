using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenThinking.Ui
{
    public interface ICommand<TArg> : ICommand
    {
        bool CanExecute(TArg parameter);

        void Execute(TArg parameter);
    }
}