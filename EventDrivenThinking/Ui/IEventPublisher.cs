namespace EventDrivenThinking.Ui
{
    public interface IEventPublisher<in T>
    {
        void Publish(T args);
    }
}