using System.Text;
using System.Windows.Input;
using EventDrivenThinking.EventInference.Abstractions.Read;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;
using EventDrivenThinking.EventInference.Abstractions.Write;
using EventDrivenThinking.EventInference.InMemory;
using EventDrivenThinking.EventInference.QueryProcessing;

namespace EventDrivenThinking.App.Configuration
{
    public class WpfBootstrapper : Bootstrapper
    {
        protected override void RegisterServices()
        {
            base.RegisterServices();
            _collection.TryAddSingleton<IQueryInvoker, QueryInvoker>();
            _collection.TryAddSingleton(typeof(IQueryEngine<>), typeof(QueryEngine<>));
            _collection.TryAddSingleton<IInMemoryProjectionStreamRegister, InMemoryProjectionStreamRegister>();
        }

        public WpfBootstrapper(ILogger logger, IServiceCollection collection) : base(logger, collection)
        {
        }
    }
}
