namespace EventDrivenThinking.App.Configuration
{
    public interface IConfigurationTask
    {
        int Order { get; }
        void Run();
    }
}