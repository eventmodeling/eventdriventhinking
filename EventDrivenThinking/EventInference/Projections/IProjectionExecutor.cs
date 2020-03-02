namespace EventDrivenThinking.EventInference.Projections
{
    public interface IProjectionExecutor<in TModel> : ISubscriptionConsumer
    {
        void Configure(TModel model);
    }
}