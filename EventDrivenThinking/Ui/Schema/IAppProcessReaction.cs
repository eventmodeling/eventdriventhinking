using System;

namespace EventDrivenThinking.Ui.Schema
{
    public interface IAppProcessReaction
    {
        ReactionReason Reason { get; }
        ReactionSource Source { get; }
        Type TriggeringType { get; }
    }
}