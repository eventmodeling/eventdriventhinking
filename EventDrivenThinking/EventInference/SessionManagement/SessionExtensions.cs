using System.Linq;
using EventDrivenThinking.EventInference.Models;

namespace EventDrivenThinking.EventInference.SessionManagement
{
    public static class SessionExtensions
    {
        /// <summary>
        /// We send events only if client hadn't subscribed for them.a
        /// </summary>
        /// <param name="session"></param>
        /// <param name="events"></param>
        public static void SendEvents(this ISession session, EventEnvelope[] events)
        {
            foreach (var i in events)
            {
                if(session.Subscriptions.All(x=>!x.Events.Contains(i.Event.GetType())))
                    session.SendEventCore(i.Metadata, i.Event);
            }
        }
    }
}