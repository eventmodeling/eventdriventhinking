using System;
using System.Collections.Generic;
using System.IO;
using Carter;
using Carter.Request;
using Carter.Response;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.EventStore;
using EventDrivenThinking.EventInference.Schema;
using EventDrivenThinking.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;

namespace EventDrivenThinking.Carter
{
    // TODO: Optimize - first execution should cache further POST/PUT/GET configurations. 
    // On startup we should fail fast if something go wrong
    // But in runtime we should not resolve everything because of performance.
    /// <summary>
    /// Obsolete most likely
    /// </summary>
    /// <typeparam name="TAggregate"></typeparam>
    internal class AggregateModule<TAggregate> : CarterModule where TAggregate : IAggregate
    {

        public AggregateModule(IAggregateSchema<TAggregate> schema,
            ICommandDispatcher commandDispatcher,
            ILogger logger,
            IServiceProvider serviceProvider)
        {
            foreach (var i in schema.Commands)
            {
                //TODO: should cache ctor for performance
                var binderType = typeof(PostBinder<>).MakeGenericType(typeof(TAggregate), i.Type);
                var binder = Ctor<IBinder>.Create(binderType);
                binder.BindCommand(schema, this, commandDispatcher, logger);
            }

            Get(schema.Category + "/{id:Guid}", async (request, response) =>
            {
                var stream = serviceProvider.GetService<IAggregateEventStream<TAggregate>>();
                Guid id = request.RouteValues.As<Guid>("id");
                var events = new List<IEvent>();
                await foreach (var e in stream.Get(id)) events.Add(e);
                await response.AsJson(events.ToArray());
            });
        }

        private interface IBinder
        {
            void BindCommand(IAggregateSchema schema, AggregateModule<TAggregate> module,
                ICommandDispatcher handlerFactory, ILogger logger);
        }

        private class PostBinder<TCommand> : IBinder
            where TCommand : ICommand
        {
            public void BindCommand(IAggregateSchema schema,
                AggregateModule<TAggregate> module,
                ICommandDispatcher commandDispatcher, ILogger logger)
            {
                var actionName = ServiceConventions.GetActionNameFromCommand(typeof(TCommand));
                module.Post($"{schema.Category}/{actionName}/{{id:Guid}}",
                    async (request, response) =>
                    {
                        logger.Information("Handling {commandName}", typeof(TCommand).Name);
                        var stream = request.BodyReader.AsStream();
                        var streamReader = new StreamReader(stream);
                        var stringContent = await streamReader.ReadToEndAsync();
                        var cmd = JsonConvert.DeserializeObject<TCommand>(stringContent);

                        Guid id = request.RouteValues.As<Guid>("id");
                        
                        await commandDispatcher.Dispatch(id, cmd);
                        await response.AsJson(cmd);
                    });
            }
        }
    }
}