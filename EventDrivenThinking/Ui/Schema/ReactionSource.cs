namespace EventDrivenThinking.Ui.Schema
{
    public enum ReactionSource
    {
        /// <summary>
        /// Reaction is triggered by server.
        /// </summary>
        Server, 
        /// <summary>
        /// Reaction is triggered by UI, server does not invokes this reaction
        /// </summary>
        UiOnly
    }
}