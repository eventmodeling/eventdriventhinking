using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EventDrivenThinking.EventInference.Models
{
    public class LinkMetadata
    {
        public LinkMetadata(Type projectionType, Guid correlationId, Guid projectionVersion)
        {
            ProjectionType = projectionType;
            CorrelationId = correlationId;
            ProjectionVersion = projectionVersion;
        }
        public Guid ProjectionVersion { get; set; }
        public Type ProjectionType { get; set; }

        [JsonProperty("$correlationId")]
        public Guid CorrelationId { get; set; }
    }
    public class EventMetadata
    {
        public EventMetadata()
        {
            
        }
        public EventMetadata(Guid aggregateId, Type aggregateType, Guid correlationId, ulong version)
        {
            AggregateId = aggregateId;
            AggregateType = aggregateType;
            CorrelationId = correlationId;
            TimeStamp = DateTimeOffset.Now;
            Version = version;
        }

        [JsonProperty("$correlationId")]
        public Guid CorrelationId { get; set; }

        //[JsonProperty("tmSp")]
        public DateTimeOffset TimeStamp { get; set; }

        //[JsonProperty("agId")]
        public Guid AggregateId { get; set; }

        //[JsonProperty("agTp")]
        public Type AggregateType { get; set; }

        // should do it somehow another way
        //[JsonProperty("u")]
        public Guid UserId { get; set; }

       
        //[JsonProperty("v")]
        [JsonIgnore]
        public ulong Version { get; set; }

    }
}