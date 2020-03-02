using Microsoft.Extensions.DependencyInjection;

namespace EventDrivenUi.Tests
{
    interface IApp
    {
        void InitializeContainer(IServiceCollection collection);
        void ConfigurePlumbing();
        void ConnectPipes();
    }
}