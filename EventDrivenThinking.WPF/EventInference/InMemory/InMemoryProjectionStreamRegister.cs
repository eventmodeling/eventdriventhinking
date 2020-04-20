using System;
using System.Collections.Concurrent;
using EventDrivenThinking.EventInference.Abstractions.Read;
using EventDrivenThinking.EventInference.EventStore;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.EventInference.InMemory
{
    public interface IInMemoryProjectionStreamRegister
    {
        IProjectionEventStream<TProjection> CreateOrGet<TProjection>() where TProjection : IProjection;
        IProjectionEventStream CreateOrGet(Type projectionType);
    }

    public class InMemoryProjectionStreamRegister : IInMemoryProjectionStreamRegister
    {
        private readonly ConcurrentDictionary<Type, IProjectionEventStream> _streams;
        private readonly IServiceProvider _serviceProvider;
        public InMemoryProjectionStreamRegister(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _streams = new ConcurrentDictionary<Type, IProjectionEventStream>();
        }
        public IProjectionEventStream CreateOrGet(Type projectionType)
        {
            var srcType = typeof(IProjectionEventStream<>).MakeGenericType(projectionType);
            var stream = _streams.GetOrAdd(srcType,
                (type) =>
                {
                    var implType = typeof(InMemoryProjectionEventStream<>).MakeGenericType(projectionType);
                    return (IProjectionEventStream)ActivatorUtilities.CreateInstance(_serviceProvider, implType);
                });
            return stream;
        }
        public IProjectionEventStream<TProjection> CreateOrGet<TProjection>() where TProjection : IProjection
        {
            var stream = _streams.GetOrAdd(typeof(TProjection),
                (type) =>
                {
                    return ActivatorUtilities.CreateInstance<InMemoryProjectionEventStream<TProjection>>(
                        _serviceProvider);
                });
            return (IProjectionEventStream<TProjection>) stream;
        }
    }
}