using System.Windows.Input;

namespace EventDrivenThinking.Ui
{
    public interface ICommandsCollection
    {
        ICommandsCollection Add(string name, ICommand cmd);
    }
}