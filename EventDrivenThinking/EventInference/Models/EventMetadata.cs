using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EventDrivenThinking.EventInference.Models
{
    public class EventMetadata
    {
        public EventMetadata()
        {
            
        }
        public EventMetadata(Guid aggregateId, Type aggregateType, Guid correlationId)
        {
            AggregateId = aggregateId;
            AggregateType = aggregateType;
            CorrelationId = correlationId;
            TimeStamp = DateTimeOffset.Now;
        }

        [JsonProperty("$correlationId")]
        public Guid CorrelationId { get; set; }

        public DateTimeOffset TimeStamp { get; set; }

        public Guid AggregateId { get; set; }
        public Type AggregateType { get; set; }

        // should do it somehow another way
        public Guid UserId { get; set; }
    }
}