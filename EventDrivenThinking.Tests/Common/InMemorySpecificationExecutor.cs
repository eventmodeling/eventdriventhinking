using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.Schema;

#pragma warning disable 1998
namespace EventDrivenThinking.Tests.Common
{
    public interface ISpecificationExecutor : IDisposable
    {
        void Init(IAggregateSchemaRegister aggregateSchemaRegister);
        IAsyncEnumerable<(Guid, IEvent)> GetEmittedEvents();
        Task ExecuteCommand(IClientCommandSchema metadata, Guid aggregateId, ICommand cmd);
        Task AppendFact(Guid aggregateId, IEvent ev);
    }
    public class InMemorySpecificationExecutor : ISpecificationExecutor
    {

        public InMemorySpecificationExecutor()
        {
            _eventsInPast = new List<(Guid, IEvent)>();
            _emittedEvents = new List<(Guid, IEvent)>();
        }
        private readonly List<(Guid, IEvent)> _eventsInPast;
        private readonly List<(Guid, IEvent)> _emittedEvents;
        private IAggregateSchemaRegister _aggregateSchemaRegister;

        public void Init(IAggregateSchemaRegister aggregateSchemaRegister)
        {
            this._aggregateSchemaRegister = aggregateSchemaRegister;
        }

        public async IAsyncEnumerable<(Guid, IEvent)> GetEmittedEvents()
        {
            foreach (var i in _emittedEvents)
                yield return i;
        }

        public async Task ExecuteCommand(IClientCommandSchema metadata, Guid aggregateId, ICommand cmd)
        {
            var aggregateType = _aggregateSchemaRegister.FindAggregateByCommand(metadata.Type);
            IAggregate aggregate = (IAggregate)Activator.CreateInstance(aggregateType.Type);
            aggregate.Id = aggregateId;
            aggregate.Rehydrate(_eventsInPast.Union(_emittedEvents)
                .Where(x => x.Item1 == aggregateId)
                .Select(x => x.Item2));

            var newEvents = aggregate.Execute(cmd)
                .Select(x => (aggregateId, x));

            _emittedEvents.AddRange(newEvents);
        }

        public async Task AppendFact(Guid aggregateId, IEvent ev)
        {
            _eventsInPast.Add((aggregateId, ev));
        }

        public void Dispose()
        {
        }
    }
}