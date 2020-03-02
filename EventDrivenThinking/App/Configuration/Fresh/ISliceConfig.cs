using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventDrivenThinking.EventInference.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenThinking.App.Configuration.Fresh
{
    public interface ISliceStartup
    {
        void RegisterServices(IServiceCollection serviceCollection);
        Task ConfigureServices(IServiceProvider serviceProvider);
    }
    public interface ISliceStartup<in T> : ISliceStartup
    {
        void Initialize(IEnumerable<T> processes);
    }

    public interface IProjectionSliceStartup : ISliceStartup<IProjectionSchema>
    {
    }

   
    public interface IProcessorSliceStartup : ISliceStartup<IProcessorSchema>
    {
    }

    public interface ICommandInvocationSliceStartup : ISliceStartup<IClientCommandSchema>
    {
    }

    public interface IAggregateSliceStartup : ISliceStartup<IAggregateSchema>
    {
    }

    public interface IServiceExtensionConfigProvider
    {
        void Register(IServiceExtensionProvider services);
    }
}