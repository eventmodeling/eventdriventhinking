using System;

namespace EventDrivenThinking.EventInference.Abstractions
{
    public class MarkupAttribute:Attribute {
        public MarkupAttribute(Type serviceType)
        {
            ServiceType = serviceType;
        }

        public Type ServiceType { get; private set; } }
}