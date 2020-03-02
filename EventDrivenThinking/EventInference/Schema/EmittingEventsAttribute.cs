using System;
using System.Linq;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;

namespace EventDrivenThinking.EventInference.Schema
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class EmittingEventsAttribute : Attribute
    {
        public EmittingEventsAttribute(params Type[] eventTypes)
        {
            if(eventTypes.Length == 0) throw new ArgumentException("Events cannot be empty.");
            if(eventTypes.Any(x=> !typeof(IEvent).IsAssignableFrom(x))) throw new ArgumentException("All event types must implement IEvent.");
            EventTypes = eventTypes;
        }

        public Type[] EventTypes { get; private set; }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class PublishingCommandsAttribute : Attribute
    {
        public PublishingCommandsAttribute(params Type[] eventTypes)
        {
            if (eventTypes.Length == 0) throw new ArgumentException("Events cannot be empty.");
            if (eventTypes.Any(x => !typeof(ICommand).IsAssignableFrom(x))) throw new ArgumentException("All event types must implement IEvent.");
            EventTypes = eventTypes;
        }

        public Type[] EventTypes { get; private set; }
    }
}