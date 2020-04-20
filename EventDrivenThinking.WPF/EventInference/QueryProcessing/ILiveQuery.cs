using System;
using EventDrivenThinking.EventInference.Abstractions.Read;

namespace EventDrivenThinking.EventInference.QueryProcessing
{
    interface ILiveQuery
    {
        void Load(IModel model);
        IQuery Query { get; }
        object Result { get; }
        Guid? PartitionId { get; }
        QueryOptions Options { get; }
    }
}