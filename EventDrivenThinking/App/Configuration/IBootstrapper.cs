using System;
using System.Threading.Tasks;

namespace EventDrivenThinking.App.Configuration
{
    public interface IBootstrapper
    {
        Task Configure(IServiceProvider serviceProvider);
    }
}