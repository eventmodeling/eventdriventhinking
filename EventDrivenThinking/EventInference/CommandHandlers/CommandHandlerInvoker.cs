using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions;
using EventDrivenThinking.EventInference.Abstractions.Write;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.EventInference.CommandHandlers
{

    public class CommandHandlerInvoker<TCommand> : ICommandInvoker<TCommand> where TCommand : ICommand
    {
        private readonly IServiceProvider _serviceProvider;
        
        public CommandHandlerInvoker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public async Task Invoke(Guid id, TCommand cmd)
        {
            using (var scope = _serviceProvider.CreateScope())
                await scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand>>().When(id, cmd);
        }
    }
}