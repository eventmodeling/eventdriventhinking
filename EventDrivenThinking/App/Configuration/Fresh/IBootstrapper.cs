using System;
using System.Threading.Tasks;

namespace EventDrivenThinking.App.Configuration.Fresh
{
    public interface IBootstrapper
    {
        Task Configure(IServiceProvider serviceProvider);
    }
}