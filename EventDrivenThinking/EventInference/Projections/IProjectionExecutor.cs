using System;
using EventDrivenThinking.EventInference.EventStore;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.EventInference.Projections
{
    public interface IProjectionExecutor<in TModel> : ISubscriptionConsumer
    {
        void Configure(TModel model);
    }

    public interface IProjectionEventStreamRepository
    {
        IProjectionEventStream GetStream(Type projection);
    }

    class ProjectionEventStreamRepository : IProjectionEventStreamRepository
    {
        private readonly IServiceProvider _serviceProvider;

        public ProjectionEventStreamRepository(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IProjectionEventStream GetStream(Type projection)
        {
            return (IProjectionEventStream)_serviceProvider.GetRequiredService(typeof(IProjectionEventStream<>).MakeGenericType(projection));
        }
    }
}