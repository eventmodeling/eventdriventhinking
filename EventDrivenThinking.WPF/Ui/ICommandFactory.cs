using System.Reflection;
using System.Windows.Input;

namespace EventDrivenThinking.Ui
{
    public interface ICommandFactory<T>
    {
        string CommandName { get; }
        void Configure(MethodInfo minfo);
        ICommand Create(T vm);
    }
}