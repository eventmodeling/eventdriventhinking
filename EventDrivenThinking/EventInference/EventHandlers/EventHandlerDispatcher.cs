using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.EventInference.EventHandlers
{
    /// <summary>
    /// Makes sense only when live.
    /// </summary>
    public class EventHandlerDispatcher : IEventHandlerDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public EventHandlerDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Dispatch<TEvent>(EventMetadata m, TEvent ev)
            where TEvent : IEvent
        {
            //TODO: Handle exceptions
            using (var scope = _serviceProvider.CreateScope())
            {
                var handlers = scope.ServiceProvider
                    .GetRequiredService<IEnumerable<IEventHandler<TEvent>>>()
                    .ToArray();

                //foreach (var i in handlers)
                //    await i.Given(m, ev);

                Task[] tasks = new Task[handlers.Length];
                for (int i = 0; i < handlers.Length; i++)
                {
                    tasks[i] = handlers[i].Execute(m, ev);
                }
                await Task.WhenAll(tasks);
            }
        }
    }
}