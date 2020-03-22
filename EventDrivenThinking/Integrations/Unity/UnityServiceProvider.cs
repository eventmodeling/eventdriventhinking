using System;
using Microsoft.Extensions.DependencyInjection;
using Unity;

namespace EventDrivenThinking.Integrations.Unity
{
    public class UnityServiceProvider : IServiceProvider
    {
        private readonly IUnityContainer _container;

        public UnityServiceProvider(IUnityContainer container)
        {
            _container = container;
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return _container.Resolve(serviceType);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
    public class UnityServiceScopeFactory : IServiceScopeFactory
    {
        private readonly IUnityContainer container;

        public UnityServiceScopeFactory(IUnityContainer container)
        {
            this.container = container;
        }

        public IServiceScope CreateScope()
        {
            return new UnityServiceScope(CreateChildContainer());
        }

        private IUnityContainer CreateChildContainer()
        {
            var child = container.CreateChildContainer();
            return child;
        }
    }
    public class UnityServiceScope : IServiceScope
    {
        private readonly IUnityContainer container;
        private readonly IServiceProvider provider;

        public UnityServiceScope(IUnityContainer container)
        {
            this.container = container;
            provider = container.Resolve<IServiceProvider>();
        }

        public IServiceProvider ServiceProvider
        {
            get { return provider; }
        }

        public void Dispose()
        {
            container.Dispose();
        }
    }
}