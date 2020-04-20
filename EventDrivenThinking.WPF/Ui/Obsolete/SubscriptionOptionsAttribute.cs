using System;
using Prism.Events;

namespace EventDrivenThinking.Ui.Obsolete
{
    public class SubscriptionOptionsAttribute : Attribute
    {
        public readonly ThreadOption Option;
        public readonly SubscriptionScope Scope;

        public SubscriptionOptionsAttribute(ThreadOption option, SubscriptionScope scope = SubscriptionScope.EventType)
        {
            Option = option;
        }
    }
}