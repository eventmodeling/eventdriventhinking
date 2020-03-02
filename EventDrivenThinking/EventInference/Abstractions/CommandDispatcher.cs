using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Abstractions.Write;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.EventInference.Abstractions
{
    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private static readonly ConcurrentDictionary<Type, Func<Guid, ICommand, Task>> cache = new ConcurrentDictionary<Type, Func<Guid, ICommand, Task>>();

        public CommandDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private Func<Guid, ICommand, Task> BuildWhenFunc(Type commandType)
        {
            var instanceParam = Expression.Constant(this);
            var idParam = Expression.Parameter(typeof(Guid), "id");
            var cmdParam = Expression.Parameter(typeof(ICommand), "cmd");
            var methodInfo = typeof(CommandDispatcher).GetMethod(nameof(Dispatch)).MakeGenericMethod(commandType);

            var callExpression = Expression.Call(instanceParam, methodInfo, idParam, Expression.Convert(cmdParam, commandType));
            var lambda = Expression.Lambda<Func<Guid, ICommand, Task>>(callExpression, idParam, cmdParam);
            return lambda.Compile();
        }
        public async Task Dispatch<TCommand>(Guid id, TCommand cmd) where TCommand:ICommand
        {
            if (typeof(TCommand) == typeof(ICommand))
            {
                await cache.GetOrAdd(cmd.GetType(), BuildWhenFunc)(id, cmd);
            }
            else
            {
                await _serviceProvider.GetRequiredService<ICommandInvoker<TCommand>>().Invoke(id, cmd);
            }
        }
    }
}