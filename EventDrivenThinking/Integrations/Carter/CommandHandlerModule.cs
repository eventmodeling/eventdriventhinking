using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using Carter;
using Carter.Request;
using Carter.Response;
using EventDrivenThinking.App.Configuration.Server;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.CommandHandlers;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;

namespace EventDrivenThinking.Integrations.Carter
{
    internal class CommandHandlerModule<TCommand> : CarterModule where TCommand : ICommand
    {
        private static readonly string Url;

        static CommandHandlerModule()
        {
            var commandType = typeof(TCommand);
            var actionName = ServiceConventions.GetActionNameFromCommand(commandType);
            string category = ServiceConventions.GetCategoryFromNamespace(commandType.Namespace);
            Url = $"{category}/{actionName}";
        }

        public CommandHandlerModule(IServiceProvider serviceProvider, ILogger logger)
        {
            Post($"{Url}/{{id:Guid}}",
                async (request, response) =>
                {
                    // Here we are in scope and serviceProvider is in scope mode.
                    logger.Information("Handling {commandName}", typeof(TCommand).Name);
                    var stream = request.BodyReader.AsStream();
                    var streamReader = new StreamReader(stream);
                    var stringContent = await streamReader.ReadToEndAsync();
                    var cmd = JsonConvert.DeserializeObject<TCommand>(stringContent);
                    
                    Guid id = request.RouteValues.As<Guid>("id");
                    var httpSession = serviceProvider.GetRequiredService<IHttpSessionManager>();
                    httpSession.Read(request);

                    var commandHandler =  serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
                    
                    await commandHandler.When(id, cmd);
                    await response.AsJson(cmd);
                });
        }
    
    }

}