using System;

namespace EventDrivenThinking.EventInference.Schema
{
    public class MarkupAttribute:Attribute {
        public MarkupAttribute(Type serviceType)
        {
            ServiceType = serviceType;
        }

        public Type ServiceType { get; private set; } }
}