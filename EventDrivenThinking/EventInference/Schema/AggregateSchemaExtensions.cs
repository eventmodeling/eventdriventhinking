using System;
using System.Linq;

namespace EventDrivenThinking.EventInference.Schema
{
    public static class AggregateSchemaExtensions
    {
        public static Type FindEventTypeByName<TAggregate>(this IAggregateSchemaRegister schemaRegister, string eventName)
        {
            var aggregateType = typeof(TAggregate);
            return FindEventTypeByName(schemaRegister, aggregateType, eventName);
        }

        public static Type FindEventTypeByName(this IAggregateSchemaRegister schemaRegister, Type aggregateType, string eventName)
        {
            foreach (var ev in schemaRegister.Events.Where(x => x.Name == eventName))
                if (schemaRegister.FindAggregateByEvent(ev).Type == aggregateType)
                    return ev;

            return null;
        }
    }
}