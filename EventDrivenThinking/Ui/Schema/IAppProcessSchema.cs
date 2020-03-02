using System;
using System.Collections.Generic;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.Ui.Schema
{
    public interface IAppProcessSchema : ISchema
    {
        Type Type { get; }
        IEnumerable<IAppProcessReaction> Reactions { get; }
    }
}