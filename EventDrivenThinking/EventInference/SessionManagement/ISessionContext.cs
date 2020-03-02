namespace EventDrivenThinking.EventInference.SessionManagement
{
    public interface ISessionContext
    {
        ISession Current();
    }
}