using System;
using System.Collections.Generic;
using System.Linq;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.Carter
{
    public class CarterModuleFactory
    {
        private readonly IEnumerable<IAggregateSchema> _aggregateSchema;
        public CarterModuleFactory(IEnumerable<IAggregateSchema> aggregateSchema)
        {
            _aggregateSchema = aggregateSchema;
        }

        public IEnumerable<Type> GetModules()
        {
            foreach (var c in _aggregateSchema.SelectMany(x=>x.Commands))
                yield return typeof(CommandHandlerModule<>).MakeGenericType(c.Type);
        }
    }
}